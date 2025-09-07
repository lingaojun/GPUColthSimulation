using UnityEngine;

/// <summary>
/// 越肩视角摄像头控制器
/// 支持WASD控制人物旋转，摄像头始终朝向人物背面
/// </summary>
public class OverShoulderCameraController : MonoBehaviour
{
    [Header("目标设置")]
    public Transform target;                    // 跟随的目标（人物）
    
    [Header("摄像头偏移")]
    public Vector3 offset = new Vector3(0.5f, 1.8f, 1.2f);  // 相对于目标的偏移
    
    [Header("控制参数")]
    public float rotationSpeed = 2f;            // 旋转速度
    public float movementSpeed = 5f;            // 移动速度
    public float mouseSensitivity = 2f;         // 鼠标灵敏度
    
    [Header("视角限制")]
    public float minVerticalAngle = -30f;       // 最小垂直角度
    public float maxVerticalAngle = 60f;        // 最大垂直角度
    
    private float currentHorizontalAngle = 0f;  // 当前水平角度
    private float currentVerticalAngle = 10f;   // 当前垂直角度
    private Vector3 lastMousePosition;          // 上一帧鼠标位置
    
    void Start()
    {
        // 初始化鼠标位置
        lastMousePosition = Input.mousePosition;
        
        // 设置初始角度
        if (target != null)
        {
            currentHorizontalAngle = target.eulerAngles.y;
        }
    }
    
    void Update()
    {
        if (target == null) return;
        
        // 处理WASD移动
        HandleMovement();
        
        // 鼠标控制已禁用
        // HandleMouseInput();
        
        // 更新摄像头位置和旋转
        UpdateCameraPosition();
    }
    
    /// <summary>
    /// 处理WASD旋转
    /// </summary>
    private void HandleMovement()
    {
        float rotation = 0f;
        
        // WASD输入控制旋转
        if (Input.GetKey(KeyCode.W))
            rotation += rotationSpeed;
        if (Input.GetKey(KeyCode.S))
            rotation -= rotationSpeed;
        if (Input.GetKey(KeyCode.A))
            rotation -= rotationSpeed;
        if (Input.GetKey(KeyCode.D))
            rotation += rotationSpeed;
        
        // 应用旋转
        if (rotation != 0f)
        {
            target.Rotate(0, rotation * Time.deltaTime, 0, Space.World);
        }
    }
    
    /// <summary>
    /// 处理鼠标输入
    /// </summary>
    private void HandleMouseInput()
    {
        // 检查鼠标右键是否按下
        if (Input.GetMouseButton(1))
        {
            // 计算鼠标移动
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            // 更新角度
            currentHorizontalAngle += mouseDelta.x * mouseSensitivity * Time.deltaTime;
            currentVerticalAngle -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
            
            // 限制垂直角度
            currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
            
            // 应用旋转到目标
            target.rotation = Quaternion.Euler(0, currentHorizontalAngle, 0);
        }
        
        // 更新鼠标位置
        lastMousePosition = Input.mousePosition;
    }
    
    /// <summary>
    /// 更新摄像头位置
    /// </summary>
    private void UpdateCameraPosition()
    {
        // 计算目标位置（相对于人物）
        Vector3 targetPosition = target.position + target.TransformDirection(offset);
        
        // 计算目标旋转（朝向人物，向下30度）
        Vector3 lookDirection = target.position - transform.position; // 从摄像机指向人物
        lookDirection.y = 0; // 保持水平视角
        Quaternion baseRotation = Quaternion.LookRotation(lookDirection);
        Quaternion downwardRotation = Quaternion.Euler(30f, 0f, 0f); // 向下30度
        Quaternion targetRotation = baseRotation * downwardRotation;
        
        // 平滑移动摄像头
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
    }
    
    /// <summary>
    /// 重置摄像头到默认位置
    /// </summary>
    public void ResetCamera()
    {
        currentHorizontalAngle = target.eulerAngles.y;
        currentVerticalAngle = 10f;
    }
}
