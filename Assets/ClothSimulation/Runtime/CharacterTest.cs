using UnityEngine;

/// <summary>
/// 人物测试脚本 - 用于测试人物创建功能
/// 这个脚本可以独立运行，不依赖布料模拟系统
/// </summary>
public class CharacterTest : MonoBehaviour
{
    /// <summary>
    /// 是否在启动时自动创建人物
    /// </summary>
    [Header("测试设置")]
    public bool createOnStart = true;
    
    /// <summary>
    /// 人物对象引用
    /// </summary>
    public GameObject character;
    
    /// <summary>
    /// 人物材质
    /// </summary>
    public Material characterMaterial;

    /// <summary>
    /// 地面对象
    /// </summary>
    public GameObject ground;
    
    /// <summary>
    /// 启动时调用
    /// </summary>
    void Start()
    {
        if (createOnStart)
        {
            CreateGround();
            CreateTestCharacter();
        }
    }
    
    /// <summary>
    /// 创建测试人物
    /// </summary>
    [ContextMenu("创建测试人物")]
    public void CreateTestCharacter()
    {
        // 如果已存在人物，先删除
        if (character != null)
        {
            DestroyImmediate(character);
        }
        
        // 创建地面（如果还没有）
        if (ground == null)
        {
            CreateGround();
        }
        
        // 创建人物
        CreateCharacter();
        
        Debug.Log("测试人物创建完成！");
        Debug.Log("使用WASD键控制人物移动");
    }
    
    /// <summary>
    /// 创建人物模型
    /// </summary>
    private void CreateCharacter()
    {
        // 创建人物根节点
        character = new GameObject("TestCharacter");
        character.transform.position = Vector3.zero;
        
        // 添加移动组件
        character.AddComponent<CharacterController>();
        
        // 创建人物各个部分
        CreateCharacterBody();
        CreateCharacterHead();
        CreateCharacterArms();
        CreateCharacterLegs();
        
        // 设置人物材质
        SetupCharacterMaterial();
        
        // 添加碰撞体
        SetupCharacterColliders();
    }
    
    /// <summary>
    /// 创建人物身体部分
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
    /// 创建人物手臂
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
    /// 创建人物腿部
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
    /// 设置人物材质
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
    /// 递归应用材质
    /// </summary>
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
    /// 删除测试人物
    /// </summary>
    [ContextMenu("删除测试人物")]
    public void DestroyTestCharacter()
    {
        if (character != null)
        {
            DestroyImmediate(character);
            character = null;
            Debug.Log("测试人物已删除");
        }
    }

    /// <summary>
    /// 创建地面
    /// </summary>
    private void CreateGround()
    {
        // 如果地面已存在，先删除
        if (ground != null)
        {
            DestroyImmediate(ground);
        }
        
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
