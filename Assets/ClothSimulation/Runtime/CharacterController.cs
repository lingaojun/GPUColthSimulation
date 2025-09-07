using UnityEngine;

/// <summary>
/// 人物控制器 - 处理人物的移动和基本行为
/// 这个类负责：
/// 1. 处理键盘输入
/// 2. 控制人物移动
/// 3. 管理人物旋转
/// 4. 处理物理交互
/// </summary>
public class CharacterController : MonoBehaviour
{
    /// <summary>
    /// 移动速度 - 人物行走的速度
    /// </summary>
    [Header("移动设置")]
    public float moveSpeed = 5f;
    
    /// <summary>
    /// 旋转速度 - 人物转向的速度
    /// </summary>
    public float rotationSpeed = 100f;
    
    /// <summary>
    /// 刚体组件 - 用于物理移动
    /// </summary>
    private Rigidbody rb;
    
    /// <summary>
    /// 初始化时调用
    /// 设置刚体组件和物理属性
    /// </summary>
    void Start()
    {
        // 获取或添加刚体组件
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // 设置刚体属性
        rb.freezeRotation = true;  // 防止人物倒下
        rb.mass = 1f;             // 设置质量
        rb.drag = 5f;             // 设置阻力，让移动更平滑
    }
    
    /// <summary>
    /// 每帧更新 - 处理输入和移动
    /// </summary>
    void Update()
    {
        HandleMovement();
    }
    
    /// <summary>
    /// 处理人物移动逻辑
    /// 根据键盘输入控制人物移动和旋转
    /// </summary>
    private void HandleMovement()
    {
        // 获取输入
        float horizontal = Input.GetAxis("Horizontal");  // A/D 或 左右箭头
        float vertical = Input.GetAxis("Vertical");      // W/S 或 上下箭头
        
        // 计算移动方向
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical).normalized;
        
        // 移动人物
        if (moveDirection.magnitude > 0.1f)
        {
            // 计算目标旋转
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            
            // 移动
            Vector3 moveVelocity = moveDirection * moveSpeed;
            rb.velocity = new Vector3(moveVelocity.x, rb.velocity.y, moveVelocity.z);
        }
        else
        {
            // 停止移动
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
    }
    
    /// <summary>
    /// 获取人物当前位置
    /// 用于其他系统（如布料模拟）获取人物位置
    /// </summary>
    /// <returns>人物世界坐标位置</returns>
    public Vector3 GetPosition()
    {
        return transform.position;
    }
    
    /// <summary>
    /// 获取人物旋转
    /// 用于其他系统获取人物朝向
    /// </summary>
    /// <returns>人物旋转四元数</returns>
    public Quaternion GetRotation()
    {
        return transform.rotation;
    }
    
    /// <summary>
    /// 设置人物位置
    /// 用于外部系统控制人物位置
    /// </summary>
    /// <param name="position">新的位置</param>
    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }
    
    /// <summary>
    /// 设置人物旋转
    /// 用于外部系统控制人物朝向
    /// </summary>
    /// <param name="rotation">新的旋转</param>
    public void SetRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }
}
