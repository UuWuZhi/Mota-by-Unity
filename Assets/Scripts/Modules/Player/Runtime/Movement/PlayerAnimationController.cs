using Modules.EventSystem.DataDefine.EventArgs;
using UnityEngine;
using VContainer;

namespace Modules.Player.Runtime.Movement
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimationController : MonoBehaviour
    {
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int VerticalHash = Animator.StringToHash("Vertical");
        private static readonly int HorizontalHash = Animator.StringToHash("Horizontal");
        [Header("组件引用")] [SerializeField] private Animator animator;

        [Header("动画配置")] public string walkAnimClipName = "Player_Walk";

        private PlayerMovement _playerMovement;
        private float _walkAnimRawLength; // 缓存Walk动画原始时长

        private void Awake()
        {
            if (animator) animator = GetComponent<Animator>();
            // 初始化动画时长（保持原逻辑）
            _walkAnimRawLength = GetAnimClipLength(walkAnimClipName);
        }

        // 订阅动画相关事件
        private void OnEnable()
        {
            if (!_playerMovement) return;
            // Debug.Log("[PlayerAnimationController]:订阅移动相关事件");
            _playerMovement.OnMoveDirectionChanged += OnMoveDirectionChanged;
            _playerMovement.OnMoveStateChanged += OnMoveStateChanged;
        }

        // 取消订阅（避免内存泄漏）
        private void OnDisable()
        {
            if (!_playerMovement) return;
            _playerMovement.OnMoveDirectionChanged -= OnMoveDirectionChanged;
            _playerMovement.OnMoveStateChanged -= OnMoveStateChanged;
        }

        [Inject]
        public void Inject(PlayerMovement playerMovement)
        {
            _playerMovement = playerMovement;
        }

        // 处理方向更新事件（更新混合树参数）
        private void OnMoveDirectionChanged(object sender, PlayerMoveDirectionChangedEventArgs args)
        {
            // Debug.Log("[PlayerAnimationController]:接收到方向更新事件");
            animator.SetFloat(HorizontalHash, args.Horizontal);
            animator.SetFloat(VerticalHash, args.Vertical);
            animator.Update(0); // 强制刷新
        }

        // 处理移动状态变更事件（启动/停止动画、调整速度）
        private void OnMoveStateChanged(object sender, PlayerMoveStateChangedEventArgs args)
        {
            // Debug.Log("[PlayerAnimationController]:接收到移动状态更新事件");
            animator.SetBool(IsMovingHash, args.IsMoving);

            if (args.IsMoving)
            {
                // 开始移动：根据移动时间计算动画速度（保持原逻辑）
                var targetSpeed = _walkAnimRawLength / args.MoveTime;
                animator.speed = targetSpeed;
            }
            else
            {
                // 停止移动：重置动画速度
                animator.speed = 1f;
            }
        }

        // 原有工具方法（获取动画时长）保持不变
        private float GetAnimClipLength(string clipName)
        {
            if (!animator || !animator.runtimeAnimatorController)
            {
                Debug.LogError("Animator组件或控制器未设置！");
                return 0.033f;
            }

            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip.name == clipName)
                    return clip.length;

            // Debug.LogError($"未找到Walk动画：{clipName}");
            return 0.033f;
        }
    }
}