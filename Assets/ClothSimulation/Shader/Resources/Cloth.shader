/// <summary>
/// 布料渲染Shader - 使用程序化渲染绘制GPU计算的布料
/// 直接从ComputeBuffer读取顶点数据，无需传统Mesh
/// </summary>
Shader "ClothSimulation/Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}  // 主纹理（当前未使用）
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }  // 不透明物体
        LOD 100
        Cull Off  // 关闭背面剔除，双面显示

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"  // 包含光照相关变量

            // ========================================
            // 输入数据 - 来自ComputeBuffer
            // ========================================
            
            /// <summary>质点位置缓冲区 - 来自Compute Shader</summary>
            StructuredBuffer<float4> _positions;
            
            /// <summary>质点法线缓冲区 - 来自Compute Shader</summary>
            StructuredBuffer<float4> _normals;

            // ========================================
            // 顶点到片段的数据结构
            // ========================================
            
            /// <summary>顶点着色器输出到片段着色器的数据</summary>
            struct v2f
            {
                float4 vertex : SV_POSITION;  // 裁剪空间位置
                float3 worldPos: TEXCOORD0;   // 世界空间位置（当前未使用）
                float3 normal: TEXCOORD1;     // 法线向量
            };

            // ========================================
            // 纹理相关
            // ========================================
            
            sampler2D _MainTex;      // 主纹理
            float4 _MainTex_ST;      // 纹理缩放和偏移

            // ========================================
            // 顶点着色器
            // ========================================
            
            /// <summary>
            /// 顶点着色器 - 将ComputeBuffer中的顶点数据转换为屏幕坐标
            /// </summary>
            /// <param name="id">顶点ID，对应质点在缓冲区中的索引</param>
            /// <returns>顶点着色器输出数据</returns>
            v2f vert (uint id : SV_VertexID)
            {
                v2f o;
                
                // 从ComputeBuffer读取顶点位置
                float4 vertex = _positions[id];
                
                // 从ComputeBuffer读取法线
                o.normal = _normals[id].xyz;
                
                // 将模型空间坐标转换为裁剪空间坐标
                o.vertex = UnityObjectToClipPos(vertex); 
                
                return o;
            }

            // ========================================
            // 片段着色器
            // ========================================
            
            /// <summary>
            /// 片段着色器 - 计算简单的光照效果
            /// 使用法线和光照方向计算漫反射
            /// </summary>
            /// <param name="i">从顶点着色器传入的数据</param>
            /// <returns>最终像素颜色</returns>
            fixed4 frag (v2f i) : SV_Target
            {
                // 获取世界空间法线
                float3 worldNormal = i.normal;
                
                // 计算法线与光照方向的点积（漫反射）
                // 使用abs确保双面光照效果
                half nl = abs(dot(worldNormal, _WorldSpaceLightPos0.xyz));
                
                // 返回光照颜色
                return nl * _LightColor0;
            }
            ENDCG
        }
    }
}
