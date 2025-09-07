using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 布料模拟示例类 - 演示如何使用ClothSimulation进行GPU布料模拟
/// 这个类负责：
/// 1. 初始化布料模拟系统
/// 2. 管理模拟参数
/// 3. 处理碰撞体（球体）
/// 4. 渲染布料
/// </summary>
public class Example : MonoBehaviour
{
    /// <summary>
    /// 布料模拟器实例 - 核心的布料物理计算类
    /// </summary>
    private ClothSimulation _simulate = new ClothSimulation();

    /// <summary>
    /// 模拟参数设置 - 可以在Inspector中调整
    /// 包括：风力、弹性系数、质量、时间步长等
    /// </summary>
    [SerializeField]
    private ClothSimulation.SimulateSetting _setting = new ClothSimulation.SimulateSetting()
    {
        wind = new Vector3(0, 0, 0),                    // 无风力
        windMultiplyAtNormal = 0.0f,                       // 风力法线乘数
        springKs = new Vector3(1000000, 1000000, 1000000), // 极大增加弹簧系数，几乎不可拉伸
        mass = 20.0f,                                    // 增加质量到十倍，提高稳定性
        stepTime = 0.0005f                               // 进一步减小时间步长，提高数值稳定性
    };

    /// <summary>
    /// 碰撞球体 - 布料会与这个球体发生碰撞
    /// </summary>
    public GameObject ball;

    /// <summary>
    /// 人物对象 - 披风将依附在这个人物上
    /// </summary>
    public GameObject character;

    /// <summary>
    /// 人物材质 - 用于渲染人物外观
    /// </summary>
    public Material characterMaterial;

    /// <summary>
    /// 地面对象 - 防止人物掉落
    /// </summary>
    public GameObject ground;

    /// <summary>
    /// 游戏对象初始化时调用
    /// 设置布料模拟参数并启动模拟
    /// </summary>
    void Awake()
    {
        // 先创建地面
        CreateGround();
        
        // 再创建人物
        CreateCharacter();
        
        // 强制设置质量参数为20 (减少质量，提高响应速度)
        _setting.mass = 20.0f;
        Debug.Log($"强制设置后的质量参数: {_setting.mass}");
        
        // 应用模拟参数设置
        _simulate.UpdateSimulateSetting(_setting);
        
        // 调试：验证质量参数
        Debug.Log($"当前质量参数: {_setting.mass}");
        
        // 设置人物到布料模拟器
        _simulate.SetCharacter(character);
        
        // 初始化碰撞球体参数
        this.UpdateBall();
        
        // 启动布料模拟协程（异步执行）
        StartCoroutine(_simulate.StartAsync());
    }

    /// <summary>
    /// 右键菜单选项 - 手动更新模拟设置
    /// 在Inspector中右键可以看到这个选项
    /// </summary>
    [ContextMenu("UpdateSetting")]
    private void UpdateSetting(){
        _simulate.UpdateSimulateSetting(_setting);
    }

    /// <summary>
    /// 更新碰撞球体的参数
    /// 将球体的位置和半径传递给布料模拟器
    /// </summary>
    void UpdateBall(){
        // 获取球体位置 (x, y, z)
        var ballParams = (Vector4)ball.transform.position;
        
        // 设置球体半径 (w分量存储半径)
        ballParams.w = ball.transform.localScale.x / 2;
        
        // 将球体参数传递给布料模拟器
        _simulate.UpdateBallParams(ballParams);
    }

    /// <summary>
    /// 每帧更新 - 实时更新碰撞球体位置
    /// 这样球体移动时布料能实时响应碰撞
    /// </summary>
    void Update(){
        this.UpdateBall();
    }

    /// <summary>
    /// 对象销毁时调用 - 清理资源
    /// 释放GPU内存缓冲区，防止内存泄漏
    /// </summary>
    void OnDestroy(){
        _simulate.Dispose();
    }

    /// <summary>
    /// Unity渲染回调 - 每帧绘制布料
    /// 使用程序化渲染直接绘制布料网格
    /// </summary>
    void OnRenderObject(){
        _simulate.Draw();
    }

    /// <summary>
    /// 创建人物模型
    /// 使用Unity内置的Primitive对象组合成完整的人物
    /// </summary>
    private void CreateCharacter()
    {
        // 创建人物根节点
        character = new GameObject("Character");
        character.transform.position = Vector3.zero;
        
        // 添加移动组件
        character.AddComponent<CharacterController>();
        
        // 创建人物各个部分
        CreateCharacterBody();
        CreateCharacterHead();
        CreateCharacterArms();
        CreateCharacterLegs();
        
        // 创建背部碰撞体
        CreateBackCollider(); // 重新启用背部碰撞体
        
        // 创建布料绑定点标记球
        CreateClothBindingMarkers();
        
        // 设置越肩视角摄像头
        SetupOverShoulderCamera();
        
        // 设置人物材质
        SetupCharacterMaterial();
        
        // 添加碰撞体
        SetupCharacterColliders();
    }

    /// <summary>
    /// 创建人物身体部分（躯干、脖子、肩膀）
    /// </summary>
    private void CreateCharacterBody()
    {
        // 创建身体（躯干）
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(character.transform);
        body.transform.localPosition = new Vector3(0, 1.0f, 0);
        body.transform.localScale = new Vector3(0.8f, 1.2f, 0.4f);
        
        // 创建脖子
        GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        neck.name = "Neck";
        neck.transform.SetParent(character.transform);
        neck.transform.localPosition = new Vector3(0, 1.8f, 0);
        neck.transform.localScale = new Vector3(0.3f, 0.2f, 0.3f);
        
        // 创建肩膀
        GameObject shoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shoulder.name = "Shoulder";
        shoulder.transform.SetParent(character.transform);
        shoulder.transform.localPosition = new Vector3(0, 1.9f, 0);
        shoulder.transform.localScale = new Vector3(1.2f, 0.3f, 0.6f);
    }

    /// <summary>
    /// 创建人物头部
    /// </summary>
    private void CreateCharacterHead()
    {
        // 创建头部
        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = "Head";
        head.transform.SetParent(character.transform);
        head.transform.localPosition = new Vector3(0, 2.3f, 0);
        head.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
    }

    /// <summary>
    /// 创建人物手臂（左臂和右臂）
    /// </summary>
    private void CreateCharacterArms()
    {
        // 左臂上臂
        GameObject leftUpperArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leftUpperArm.name = "LeftUpperArm";
        leftUpperArm.transform.SetParent(character.transform);
        leftUpperArm.transform.localPosition = new Vector3(-0.7f, 1.6f, 0);
        leftUpperArm.transform.localScale = new Vector3(0.3f, 0.8f, 0.3f);
        leftUpperArm.transform.localRotation = Quaternion.Euler(0, 0, 90);
        
        // 左臂下臂
        GameObject leftLowerArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leftLowerArm.name = "LeftLowerArm";
        leftLowerArm.transform.SetParent(character.transform);
        leftLowerArm.transform.localPosition = new Vector3(-1.1f, 1.2f, 0);
        leftLowerArm.transform.localScale = new Vector3(0.25f, 0.7f, 0.25f);
        leftLowerArm.transform.localRotation = Quaternion.Euler(0, 0, 90);
        
        // 左手
        GameObject leftHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftHand.name = "LeftHand";
        leftHand.transform.SetParent(character.transform);
        leftHand.transform.localPosition = new Vector3(-1.4f, 0.9f, 0);
        leftHand.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        
        // 右臂上臂
        GameObject rightUpperArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rightUpperArm.name = "RightUpperArm";
        rightUpperArm.transform.SetParent(character.transform);
        rightUpperArm.transform.localPosition = new Vector3(0.7f, 1.6f, 0);
        rightUpperArm.transform.localScale = new Vector3(0.3f, 0.8f, 0.3f);
        rightUpperArm.transform.localRotation = Quaternion.Euler(0, 0, 90);
        
        // 右臂下臂
        GameObject rightLowerArm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rightLowerArm.name = "RightLowerArm";
        rightLowerArm.transform.SetParent(character.transform);
        rightLowerArm.transform.localPosition = new Vector3(1.1f, 1.2f, 0);
        rightLowerArm.transform.localScale = new Vector3(0.25f, 0.7f, 0.25f);
        rightLowerArm.transform.localRotation = Quaternion.Euler(0, 0, 90);
        
        // 右手
        GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightHand.name = "RightHand";
        rightHand.transform.SetParent(character.transform);
        rightHand.transform.localPosition = new Vector3(1.4f, 0.9f, 0);
        rightHand.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
    }

    /// <summary>
    /// 创建背部碰撞体 - 防止布料穿模
    /// </summary>
    private void CreateBackCollider()
    {
        // 使用Unity内置的立方体创建背部碰撞体
        GameObject backCollider = GameObject.CreatePrimitive(PrimitiveType.Cube);
        backCollider.name = "BackCollider";
        backCollider.transform.SetParent(character.transform);
        backCollider.transform.localPosition = new Vector3(0, 1.5f, -0.2f); // 在背部位置，更靠近人物
        backCollider.transform.localScale = new Vector3(1.2f, 2.0f, 0.05f); // 覆盖整个背部，调薄一点
        
        // 设置透明材质
        Material backMaterial = new Material(Shader.Find("Standard"));
        backMaterial.color = new Color(1, 0, 0, 0.5f); // 半透明红色，便于调试
        backMaterial.SetFloat("_Mode", 3); // 设置为透明模式
        backMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        backMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        backMaterial.SetInt("_ZWrite", 0);
        backMaterial.DisableKeyword("_ALPHATEST_ON");
        backMaterial.EnableKeyword("_ALPHABLEND_ON");
        backMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        backMaterial.renderQueue = 3000;
        
        // 应用材质
        Renderer renderer = backCollider.GetComponent<Renderer>();
        renderer.material = backMaterial;
        
        Debug.Log($"背部碰撞体创建完成！位置: {backCollider.transform.position}, 尺寸: {backCollider.transform.localScale}");
    }
    
    /// <summary>
    /// 创建布料绑定点标记球 - 显示布料固定点的位置
    /// </summary>
    private void CreateClothBindingMarkers()
    {
        // 获取布料网格尺寸（与Compute Shader中的size一致）
        int clothWidth = 8;  // 与Compute Shader中的_vertexCountPerDim一致
        int clothHeight = 8;
        
        // 创建标记球容器
        GameObject markerContainer = new GameObject("ClothBindingMarkers");
        markerContainer.transform.SetParent(character.transform);
        
        // 创建红色材质
        Material markerMaterial = new Material(Shader.Find("Standard"));
        markerMaterial.color = Color.red;
        markerMaterial.SetFloat("_Metallic", 0.0f);
        markerMaterial.SetFloat("_Glossiness", 0.8f);
        
        // 为第一行（固定点）创建标记球
        for (int x = 0; x < clothWidth; x++)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = $"BindingMarker_{x}";
            marker.transform.SetParent(markerContainer.transform);
            
            // 设置标记球尺寸
            marker.transform.localScale = Vector3.one * 0.1f; // 直径0.1米
            
            // 计算标记球位置（与Compute Shader中的计算一致）
            float widthRatio = (float)x / (clothWidth - 1);  // 0到1
            float currentWidth = 1.1f;  // 肩膀宽度，与Compute Shader一致
            
            // 计算相对于肩膀的局部位置
            Vector3 localPos = new Vector3(
                (widthRatio - 0.5f) * currentWidth,  // x: 左右位置
                1.9f,                                 // y: 肩膀高度
                -0.5f                                 // z: 前后位置，向后0.5米
            );
            
            // 设置标记球位置
            marker.transform.localPosition = localPos;
            
            // 应用红色材质
            Renderer renderer = marker.GetComponent<Renderer>();
            renderer.material = markerMaterial;
            
            // 移除碰撞体，避免干扰
            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyImmediate(collider);
            }
        }
        
        Debug.Log($"布料绑定点标记球创建完成！共{clothWidth}个标记球");
    }
    
    /// <summary>
    /// 设置越肩视角摄像头
    /// </summary>
    private void SetupOverShoulderCamera()
    {
        // 获取主摄像头
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            // 如果没有主摄像头，创建一个
            GameObject cameraObj = new GameObject("Main Camera");
            mainCamera = cameraObj.AddComponent<Camera>();
            cameraObj.tag = "MainCamera";
        }
        
        // 设置摄像头位置（越肩视角）
        mainCamera.transform.SetParent(character.transform);
        mainCamera.transform.localPosition = new Vector3(4.0f, 4.0f, 5.0f); // 右肩后方5米
        mainCamera.transform.localRotation = Quaternion.Euler(0f, 0f, 0f); // 初始朝向
        
        // 设置摄像头参数
        mainCamera.fieldOfView = 60f;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = 1000f;
        
        // 添加摄像头控制脚本
        OverShoulderCameraController cameraController = mainCamera.gameObject.GetComponent<OverShoulderCameraController>();
        if (cameraController == null)
        {
            cameraController = mainCamera.gameObject.AddComponent<OverShoulderCameraController>();
        }
        
        // 设置摄像头控制参数
        cameraController.target = character.transform;
        cameraController.offset = new Vector3(0.5f, 1.8f, 5.0f);
        cameraController.rotationSpeed = 2f;
        cameraController.movementSpeed = 5f;
        
        Debug.Log("越肩视角摄像头设置完成！");
    }
    

    /// <summary>
    /// 创建人物腿部（左腿和右腿）
    /// </summary>
    private void CreateCharacterLegs()
    {
        // 左大腿
        GameObject leftThigh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leftThigh.name = "LeftThigh";
        leftThigh.transform.SetParent(character.transform);
        leftThigh.transform.localPosition = new Vector3(-0.3f, 0.3f, 0);
        leftThigh.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
        
        // 左小腿
        GameObject leftCalf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        leftCalf.name = "LeftCalf";
        leftCalf.transform.SetParent(character.transform);
        leftCalf.transform.localPosition = new Vector3(-0.3f, -0.3f, 0);
        leftCalf.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);
        
        // 左脚
        GameObject leftFoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftFoot.name = "LeftFoot";
        leftFoot.transform.SetParent(character.transform);
        leftFoot.transform.localPosition = new Vector3(-0.3f, -0.8f, 0.2f);
        leftFoot.transform.localScale = new Vector3(0.3f, 0.2f, 0.6f);
        
        // 右大腿
        GameObject rightThigh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rightThigh.name = "RightThigh";
        rightThigh.transform.SetParent(character.transform);
        rightThigh.transform.localPosition = new Vector3(0.3f, 0.3f, 0);
        rightThigh.transform.localScale = new Vector3(0.4f, 0.8f, 0.4f);
        
        // 右小腿
        GameObject rightCalf = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        rightCalf.name = "RightCalf";
        rightCalf.transform.SetParent(character.transform);
        rightCalf.transform.localPosition = new Vector3(0.3f, -0.3f, 0);
        rightCalf.transform.localScale = new Vector3(0.35f, 0.8f, 0.35f);
        
        // 右脚
        GameObject rightFoot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightFoot.name = "RightFoot";
        rightFoot.transform.SetParent(character.transform);
        rightFoot.transform.localPosition = new Vector3(0.3f, -0.8f, 0.2f);
        rightFoot.transform.localScale = new Vector3(0.3f, 0.2f, 0.6f);
    }

    /// <summary>
    /// 设置人物材质和颜色
    /// </summary>
    private void SetupCharacterMaterial()
    {
        // 创建人物材质
        characterMaterial = new Material(Shader.Find("Standard"));
        characterMaterial.color = new Color(0.8f, 0.6f, 0.4f);  // 肤色
        characterMaterial.SetFloat("_Metallic", 0.1f);
        characterMaterial.SetFloat("_Glossiness", 0.3f);
        
        // 为所有身体部分应用材质
        ApplyMaterialToChildren(character.transform, characterMaterial);
    }

    /// <summary>
    /// 递归为所有子对象应用材质
    /// </summary>
    /// <param name="parent">父对象</param>
    /// <param name="material">要应用的材质</param>
    private void ApplyMaterialToChildren(Transform parent, Material material)
    {
        // 为当前对象应用材质
        Renderer renderer = parent.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = material;
        }
        
        // 递归为所有子对象应用材质
        for (int i = 0; i < parent.childCount; i++)
        {
            ApplyMaterialToChildren(parent.GetChild(i), material);
        }
    }

    /// <summary>
    /// 设置人物碰撞体
    /// </summary>
    private void SetupCharacterColliders()
    {
        // 为身体添加碰撞体
        GameObject body = character.transform.Find("Body").gameObject;
        CapsuleCollider bodyCollider = body.GetComponent<CapsuleCollider>();
        if (bodyCollider == null)
        {
            bodyCollider = body.AddComponent<CapsuleCollider>();
        }
        bodyCollider.radius = 0.4f;
        bodyCollider.height = 1.2f;
        
        // 为头部添加碰撞体
        GameObject head = character.transform.Find("Head").gameObject;
        SphereCollider headCollider = head.GetComponent<SphereCollider>();
        if (headCollider == null)
        {
            headCollider = head.AddComponent<SphereCollider>();
        }
        headCollider.radius = 0.3f;
    }

    /// <summary>
    /// 创建地面
    /// 防止人物和布料掉落
    /// </summary>
    private void CreateGround()
    {
        // 创建地面
        ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(0, -1, 0);
        ground.transform.localScale = new Vector3(10, 1, 10);  // 20x20的地面
        
        // 设置地面材质
        Material groundMaterial = new Material(Shader.Find("Standard"));
        groundMaterial.color = new Color(0.5f, 0.5f, 0.5f);  // 灰色
        groundMaterial.SetFloat("_Metallic", 0.0f);
        groundMaterial.SetFloat("_Glossiness", 0.1f);
        
        // 应用材质
        Renderer groundRenderer = ground.GetComponent<Renderer>();
        if (groundRenderer != null)
        {
            groundRenderer.material = groundMaterial;
        }
        
        // 确保有碰撞体
        Collider groundCollider = ground.GetComponent<Collider>();
        if (groundCollider == null)
        {
            groundCollider = ground.AddComponent<BoxCollider>();
        }
        
        Debug.Log("地面创建完成！");
    }
}
