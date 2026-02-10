using UnityEngine;
using VContainer;

// 输入管理器单例（仅负责检测输入，不关心移动逻辑）
public class MovementInputManager : MonoBehaviour
{
    [Header("输入设置")]
    [SerializeField] private float inputInterval = 0.15f;   // 输入间隔（也许有用呢？）
    [SerializeField] private bool useInputInterval = false; // 间隔开关（false=长按无间隔）
    private float _lastInputTime;                           // 上次输入时间

    public bool IsInputBlocked { get; set; } = false;       // 输入屏蔽开关（如对话/战斗时禁用移动输入）

    private EventCenter _eventCenter;
    [Inject]
    public void Inject(EventCenter eventCenter)
    {
        _eventCenter = eventCenter;
    }

    private void Update()
    {
        if (IsInputBlocked)
        {
            return; // 输入屏蔽时直接返回
        }

        // 检测上下左右输入（改用 GetKey 实现长按持续触发）
        Vector2 moveDir = Vector2.zero;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            moveDir = Vector2.up;
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            moveDir = Vector2.down;
        }
        else if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            moveDir = Vector2.left;
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            moveDir = Vector2.right;
        }

        // 有有效输入时处理
        if (moveDir != Vector2.zero)
        {
            // 根据间隔开关判断是否需要限制触发频率
            if (!useInputInterval || (Time.time - _lastInputTime >= inputInterval))
            {
                _lastInputTime = Time.time; // 更新上次输入时间（无论是否启用间隔都记录，方便后续扩展）
                _eventCenter.TriggerPlayerMoveInput(new PlayerInputEventArgs
                {
                    MoveDirection = moveDir,
                    IsValidInput = true
                });
                // if (Time.time - _lastInputTime >= inputInterval) Debug.Log("发布移动事件（方向：" + moveDir + "）");
            }
        }
    }
}