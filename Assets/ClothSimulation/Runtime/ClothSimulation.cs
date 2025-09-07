
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 布料模拟核心类 - 使用GPU Compute Shader实现高性能布料物理模拟
/// 主要功能：
/// 1. 管理GPU内存缓冲区（位置、速度、法线）
/// 2. 控制Compute Shader的执行
/// 3. 处理布料渲染
/// 4. 提供参数调整接口
/// </summary>
public class ClothSimulation
{
    /// <summary>
    /// 模拟参数设置类 - 可在Inspector中序列化显示
    /// 包含所有影响布料行为的物理参数
    /// </summary>
    [System.Serializable]
    public class SimulateSetting{
        /// <summary>风力向量 - 影响布料的飘动效果 (x,y,z) = (风向和强度)</summary>
        public Vector3 wind = new Vector3(0,0,10);
        
        /// <summary>风力在法线方向的乘数 - 控制风力对布料的影响程度</summary>
        public float windMultiplyAtNormal = 0f;
        
        /// <summary>弹簧弹性系数 - x=结构弹簧, y=剪力弹簧, z=弯曲弹簧</summary>
        public Vector3 springKs = new Vector3(25000,25000,25000);
        
        /// <summary>单个质点的质量 - 影响重力和惯性</summary>
        public float mass = 1;
        
        /// <summary>物理模拟时间步长 - 越小越稳定但计算量越大</summary>
        public float stepTime = 0.003f;
    }

    
    /// <summary>Compute Shader实例 - 缓存避免重复加载</summary>
    private static ComputeShader _cs;

    /// <summary>人物对象引用 - 用于跟踪人物位置</summary>
    private GameObject _character;

    /// <summary>Compute Shader属性 - 懒加载模式，首次访问时从Resources加载</summary>
    private static ComputeShader CS{
        get{
            if(!_cs){
                _cs = Resources.Load<ComputeShader>("ClothCS");
            }
            return _cs;
        }
    }

    /// <summary>渲染材质实例 - 缓存避免重复创建</summary>
    private static Material _material;

    /// <summary>渲染材质属性 - 懒加载模式，用于布料渲染</summary>
    private static Material material{
        get{
            if(!_material){
                _material = new Material(Shader.Find("ClothSimulation/Unlit"));
            }
            return _material;
        }
    }

    /// <summary>布料物理尺寸 - 布料网格在3D空间中的实际大小</summary>
    private float _clothSize = 1.1f;

    /// <summary>当前模拟参数设置</summary>
    private SimulateSetting _simulateSetting = new SimulateSetting();

    /// <summary>位置缓冲区 - 存储所有质点的位置信息 (float4: x,y,z,w)</summary>
    private ComputeBuffer _positionBuffer;

    /// <summary>法线缓冲区 - 存储每个质点的法线信息，用于光照计算</summary>
    private ComputeBuffer _normalBuffer;

    /// <summary>速度缓冲区 - 存储所有质点的速度信息 (float3: vx,vy,vz)</summary>
    private ComputeBuffer _velocitiesBuffer;

    /// <summary>GPU线程组大小 - X方向线程数</summary>
    private const int THREAD_X = 8;
    /// <summary>GPU线程组大小 - Y方向线程数</summary>
    private const int THREAD_Y = 8;

    /// <summary>总质点数量 - 等于 _vertexCountPerDim * _vertexCountPerDim</summary>
    private int _totalVertexCount;
    /// <summary>每个维度的质点数量 - 8x8网格 = 64个质点</summary>
    private int _vertexCountPerDim = 8;

    /// <summary>初始化状态标志 - 确保在初始化完成前不进行渲染</summary>
    private bool _initialized = false;

    /// <summary>Compute Shader中的初始化函数ID</summary>
    private int _kernelInit;
    /// <summary>Compute Shader中的速度更新函数ID</summary>
    private int _kernelStepVelocity;
    /// <summary>Compute Shader中的位置更新函数ID</summary>
    private int _kernelStepPosition;

    /// <summary>GPU线程组数量 - X方向</summary>
    private int _groupX;
    /// <summary>GPU线程组数量 - Y方向</summary>
    private int _groupY;

    /// <summary>
    /// 构造函数 - 计算GPU线程组数量
    /// 32x32网格，8x8线程组 = 4x4个线程组
    /// </summary>
    public ClothSimulation(){
        _groupX = _vertexCountPerDim / THREAD_X;  // 4个线程组
        _groupY = _vertexCountPerDim / THREAD_Y;  // 4个线程组
    }

    /// <summary>
    /// 更新模拟设置 - 外部调用接口
    /// </summary>
    /// <param name="setting">新的模拟参数</param>
    public void UpdateSimulateSetting(SimulateSetting setting){
        _simulateSetting = setting;
        this.UpdateSimulateSetting();
    }
    
    /// <summary>
    /// 将模拟参数传递给Compute Shader
    /// 每次参数改变时都需要调用此方法
    /// </summary>
    public void UpdateSimulateSetting(){
        // 准备风力参数：xyz=风向强度，w=法线乘数
        var viscousFluidArgs = (Vector4)_simulateSetting.wind;
        viscousFluidArgs.w = _simulateSetting.windMultiplyAtNormal;
        
        // 将参数传递给Compute Shader
        CS.SetVector("viscousFluidArgs", viscousFluidArgs);  // 风力参数
        CS.SetVector("springKs", _simulateSetting.springKs); // 弹性系数
        CS.SetFloat("mass", _simulateSetting.mass);          // 质点质量
    }

    /// <summary>
    /// 更新碰撞球体参数
    /// </summary>
    /// <param name="ball">球体参数：xyz=位置，w=半径</param>
    public void UpdateBallParams(Vector4 ball){
        CS.SetVector("collisionBall", ball);
    }

    /// <summary>
    /// 设置人物对象 - 用于跟踪人物位置
    /// </summary>
    /// <param name="character">人物GameObject</param>
    public void SetCharacter(GameObject character){
        _character = character;
    }

    /// <summary>
    /// 更新人物位置到GPU - 让披风固定点跟随人物移动
    /// </summary>
    private void UpdateCharacterPosition(){
        if(_character != null){
            // 获取人物肩膀位置（肩膀在人物上方1.9米处）
            Vector3 characterPos = _character.transform.position;
            Vector3 shoulderPos = characterPos + new Vector3(0, 1.9f, 0);
            
            // 获取人物旋转信息
            Quaternion characterRot = _character.transform.rotation;
            
            // 将人物位置和旋转传递给GPU
            CS.SetVector("characterPosition", shoulderPos);
            CS.SetVector("characterRotation", new Vector4(characterRot.x, characterRot.y, characterRot.z, characterRot.w));
        }
    }
 
    /// <summary>
    /// 初始化布料模拟系统
    /// 创建GPU缓冲区，设置参数，执行初始化计算
    /// </summary>
    /// <returns>异步GPU读取请求，用于确认初始化完成</returns>
    public AsyncGPUReadbackRequest Initialize(){
        // 1. 获取Compute Shader中的函数ID
        _kernelInit = CS.FindKernel("Init");           // 初始化函数
        _kernelStepVelocity = CS.FindKernel("StepV");  // 速度更新函数
        _kernelStepPosition = CS.FindKernel("StepP");  // 位置更新函数
     
        // 2. 计算网格参数
        var vertexCount = _vertexCountPerDim;          // 32
        var totalVertex = vertexCount * vertexCount;   // 1024个质点
        var L0 = _clothSize / (vertexCount - 1);       // 弹簧静止长度
        
        // 3. 设置Compute Shader参数
        CS.SetInts("size", vertexCount, vertexCount, totalVertex);  // 网格尺寸
        CS.SetVector("restLengths", new Vector3(
            L0,                    // 结构弹簧长度
            L0 * Mathf.Sqrt(2),    // 剪力弹簧长度（对角线）
            L0 * 2                 // 弯曲弹簧长度（跨一个位置）
        ));

        // 4. 应用模拟参数
        this.UpdateSimulateSetting();

        // 5. 创建GPU缓冲区
        _positionBuffer = new ComputeBuffer(totalVertex, 16);   // 位置：float4
        _velocitiesBuffer = new ComputeBuffer(totalVertex, 16); // 速度：float3+padding
        _normalBuffer = new ComputeBuffer(totalVertex, 16);     // 法线：float4

        // 6. 为所有Kernel设置缓冲区
        System.Action<int> setBufferForKernet = (k)=>{
            CS.SetBuffer(k, "velocities", _velocitiesBuffer);
            CS.SetBuffer(k, "positions", _positionBuffer);
            CS.SetBuffer(k, "normals", _normalBuffer);
        };

        setBufferForKernet(_kernelInit);
        setBufferForKernet(_kernelStepVelocity);
        setBufferForKernet(_kernelStepPosition);

        // 7. 执行初始化计算
        CS.Dispatch(_kernelInit, _groupX, _groupY, 1);

        _totalVertexCount = totalVertex;

        // 8. 创建渲染用的索引缓冲区
        CreateIndexBuffer();
        
        // 9. 设置渲染材质的数据源
        material.SetBuffer(ShaderIDs.position, _positionBuffer);
        material.SetBuffer(ShaderIDs.normals, _normalBuffer);

        // 10. 异步等待初始化完成
        return AsyncGPUReadback.Request(_positionBuffer, (req)=>{
            if(req.hasError){
                Debug.LogError("Init error");
            }
            if(req.done && !req.hasError){
                _initialized = true;  // 标记初始化完成
            }
        });
    }

    /// <summary>索引缓冲区 - 定义三角形的顶点连接关系</summary>
    GraphicsBuffer _indexBuffer;

    /// <summary>Shader属性ID缓存 - 避免字符串查找的性能开销</summary>
	static class ShaderIDs {
		public static int position = Shader.PropertyToID( "_positions" );
        public static int normals = Shader.PropertyToID( "_normals" );
	}
    
    /// <summary>
    /// 创建索引缓冲区 - 将32x32的质点网格转换为三角形网格
    /// 每个四边形由2个三角形组成，每个三角形3个顶点
    /// </summary>
    private void CreateIndexBuffer(){
        var vertexCount = _vertexCountPerDim;                    // 32
        var quadCount = (vertexCount - 1) * (vertexCount - 1);  // 31x31 = 961个四边形
        _indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, quadCount * 6, sizeof(int));
        
        int[] indicies = new int[_indexBuffer.count];  // 961 * 6 = 5766个索引
        
        // 遍历每个四边形
        for(var x = 0; x < vertexCount - 1; x++){
            for(var y = 0; y < vertexCount - 1; y++){
                // 计算当前四边形的四个顶点索引
                var vertexIndex = (y * vertexCount + x);        // 左下角
                var quadIndex = y * (vertexCount - 1) + x;      // 四边形索引
                var upVertexIndex = (vertexIndex + vertexCount); // 左上角
                var offset = quadIndex * 6;                     // 当前四边形在索引数组中的起始位置
                
                // 第一个三角形：左下 -> 右下 -> 左上
                indicies[offset] = vertexIndex; 
                indicies[offset + 1] = (vertexIndex + 1); 
                indicies[offset + 2] = upVertexIndex; 

                // 第二个三角形：左上 -> 右下 -> 右上
                indicies[offset + 3] = upVertexIndex; 
                indicies[offset + 4] = (vertexIndex + 1); 
                indicies[offset + 5] = (upVertexIndex + 1); 
            }
        }
        _indexBuffer.SetData(new List<int>(indicies));
    }


    /// <summary>
    /// 启动异步布料模拟 - 协程方式运行
    /// 每帧进行多次物理计算以保持数值稳定性
    /// </summary>
    public IEnumerator StartAsync(){
        // 1. 等待初始化完成
        yield return Initialize();
        
        float dt = 0;                                    // 累计时间
        float minDt = _simulateSetting.stepTime;         // 最小时间步长 (0.003s)
        
        // 2. 无限循环进行物理模拟
        while(true){
            dt += Time.deltaTime;  // 累计帧时间
            
            // 更新人物位置到GPU
            UpdateCharacterPosition();
            
            // 3. 子步长循环：每帧可能进行多次物理计算
            while(dt > minDt){
                CS.SetFloat("deltaTime", minDt);  // 设置时间步长
                
                // 4. 执行物理计算：先更新速度，再更新位置
                CS.Dispatch(_kernelStepVelocity, _groupX, _groupY, 1);  // 计算受力和速度
                CS.Dispatch(_kernelStepPosition, _groupX, _groupY, 1);  // 更新位置
                
                dt -= minDt;  // 消耗一个时间步长
            }
            
            yield return null;  // 等待下一帧
            AsyncGPUReadback.WaitAllRequests();  // 等待所有GPU操作完成
        }
    }

    /// <summary>
    /// 绘制布料 - 使用程序化渲染
    /// 直接从GPU缓冲区读取数据绘制，无需CPU-GPU数据传输
    /// </summary>
    public void Draw(){
        if(!_initialized){
            return;  // 初始化未完成时不绘制
        }
        material.SetPass(0);  // 设置渲染通道
        Graphics.DrawProceduralNow(MeshTopology.Triangles, _indexBuffer, _indexBuffer.count, 1);
    }

    /// <summary>
    /// 释放资源 - 清理GPU内存缓冲区
    /// 防止内存泄漏，必须在对象销毁时调用
    /// </summary>
    public void Dispose(){
        Debug.Log("release buffers");
        
        // 释放位置缓冲区
        if(_positionBuffer != null){
            _positionBuffer.Release();
            _positionBuffer = null;
        }
        
        // 释放速度缓冲区
        if(_velocitiesBuffer != null){
            _velocitiesBuffer.Release();
            _velocitiesBuffer = null;
        }
        
        // 释放索引缓冲区
        if(_indexBuffer != null){
            _indexBuffer.Release();
            _indexBuffer = null;
        }
    }
}
