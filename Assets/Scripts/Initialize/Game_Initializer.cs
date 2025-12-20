using UnityEngine;

public class GameInitializer : MonoBehaviour
{
    [Header("核心引用")]
    [SerializeField] private GameObject player; // 仅需拖入Player对象（无需其他手动设置）

    // 保留一个较小的职责：仅负责动画 speed 的本地设置与可视化相关的初始化
    private void Start()
    {
        InitAnimationSpeed();
    }

    // 核心方法：读取统一参数，计算动画速度
    private void InitAnimationSpeed()
    {
        if (player == null)
        {
            Debug.LogWarning("GameInitializer: player 引用为空，跳过动画速度初始化。");
            return;
        }

        Animator playerAnimator = player.GetComponent<Animator>();
        if (playerAnimator == null)
        {
            Debug.LogError("Player对象缺少Animator组件！");
            return;
        }

        // 动画时长已和移动时长匹配，速度设为1（正常播放）
        playerAnimator.speed = 1f;
    }
}