using UnityEngine;
using VContainer;

// 输入管理器单例（仅负责检测输入，不关心移动逻辑）
// 改进：支持“后按覆盖先按”的行为 —— 新按下的方向会覆盖当前正在按住的方向
// 并引入抽象按键 AbstractKey，首要按键为 WASD，次要按键为 ↑←↓→，在代码中统一使用抽象按键以便后续配置
public class MovementInputManager : MonoBehaviour
{
    [Header("输入设置")]
    [SerializeField] private float inputInterval = 0.15f;   // 输入间隔（也许有用呢？）
    [SerializeField] private bool useInputInterval = false; // 间隔开关（false=长按无间隔）

    public bool IsInputBlocked { get; set; } = false;       // 输入屏蔽开关（如对话/战斗时禁用移动输入）

    private PlayerMovement _playerMovement;
    [Inject]
    public void Inject(PlayerMovement playerMovement)
    {
        _playerMovement = playerMovement;
    }

    // 抽象按键枚举（UP/LEFT/DOWN/RIGHT），用于在代码中统一表示方向输入
    private enum AbstractKey
    {
        None = 0,
        Up,
        Left,
        Down,
        Right
    }

    private AbstractKey _lastPressedKey = AbstractKey.None; // 记录最后一次按下的抽象按键（使用 AbstractKey.None 表示无）
    private float _lastInputTime;                           // 上次输入时间

    private void Update()
    {
        if (IsInputBlocked)
        {
            return; // 输入屏蔽时直接返回
        }

        // 先检查是否有新按下的抽象按键（GetKeyDown），新按下的按键总是覆盖之前的按键
        AbstractKey newPressed = GetAbstractKeyDown();
        if (newPressed != AbstractKey.None)
        {
            _lastPressedKey = newPressed;
        }

        // 决定当前生效的移动方向：
        // 1) 如果最后按下的抽象按键仍在被按住，则使用它（实现后按覆盖先按）
        // 2) 否则，回退到检测当前仍被按住的任一方向（按优先级依次为 UP/DOWN/LEFT/RIGHT 的检测顺序）
        Vector2 moveDir = Vector2.zero;
        if (_lastPressedKey != AbstractKey.None && IsAbstractKeyHeld(_lastPressedKey))
        {
            moveDir = GetDirectionForAbstractKey(_lastPressedKey);
        }
        else
        {
            // Last pressed key 不再被按住，尝试找到当前仍被按住的键（回退）
            AbstractKey held = GetAnyAbstractKeyHeld();
            if (held != AbstractKey.None)
            {
                _lastPressedKey = held;
                moveDir = GetDirectionForAbstractKey(held);
            }
            else
            {
                // 没有按键被按住，清除 lastPressedKey
                _lastPressedKey = AbstractKey.None;
            }
        }

        // 有有效输入时处理
        if (moveDir != Vector2.zero)
        {
            // 根据间隔开关判断是否需要限制触发频率
            if (!useInputInterval || (Time.time - _lastInputTime >= inputInterval))
            {
                _lastInputTime = Time.time; // 更新上次输入时间（无论是否启用间隔都记录，方便后续扩展）
                if (_playerMovement != null)
                {
                    _playerMovement.HandleMoveInput(new PlayerInputEventArgs
                    {
                        MoveDirection = moveDir,
                        IsValidInput = true
                    });
                }
            }
        }
    }

    // 辅助：检测是否有某抽象按键被新按下（GetKeyDown）
    private AbstractKey GetAbstractKeyDown()
    {
        if (IsPrimaryOrSecondaryKeyDown(KeyCode.W, KeyCode.UpArrow)) return AbstractKey.Up;
        if (IsPrimaryOrSecondaryKeyDown(KeyCode.S, KeyCode.DownArrow)) return AbstractKey.Down;
        if (IsPrimaryOrSecondaryKeyDown(KeyCode.A, KeyCode.LeftArrow)) return AbstractKey.Left;
        if (IsPrimaryOrSecondaryKeyDown(KeyCode.D, KeyCode.RightArrow)) return AbstractKey.Right;
        return AbstractKey.None;
    }

    // 辅助：判断抽象按键是否仍被按住（处理首要/次要按键）
    private bool IsAbstractKeyHeld(AbstractKey key)
    {
        return key switch
        {
            AbstractKey.Up => Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
            AbstractKey.Down => Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
            AbstractKey.Left => Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
            AbstractKey.Right => Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
            _ => false,
        };
    }

    // 辅助：返回任意仍被按住的抽象按键（按优先级 UP/ DOWN/ LEFT/ RIGHT）
    private AbstractKey GetAnyAbstractKeyHeld()
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) return AbstractKey.Up;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) return AbstractKey.Down;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) return AbstractKey.Left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) return AbstractKey.Right;
        return AbstractKey.None;
    }

    // 辅助：根据抽象按键返回方向
    private Vector2 GetDirectionForAbstractKey(AbstractKey key)
    {
        return key switch
        {
            AbstractKey.Up => Vector2.up,
            AbstractKey.Down => Vector2.down,
            AbstractKey.Left => Vector2.left,
            AbstractKey.Right => Vector2.right,
            _ => Vector2.zero,
        };
    }

    // 辅助：检测某组首要/次要按键的 GetKeyDown
    private bool IsPrimaryOrSecondaryKeyDown(KeyCode primary, KeyCode secondary)
    {
        return Input.GetKeyDown(primary) || Input.GetKeyDown(secondary);
    }
}
