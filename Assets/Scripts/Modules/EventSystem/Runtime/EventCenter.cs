//全局事件管理
using System;
using UnityEngine;

// 全局事件中心（单例，统一管理所有游戏事件）
public class EventCenter : MonoBehaviour
{
    public static EventCenter Instance;
    // 层数切换相关
    public event EventHandler<LayerSwitchedEventArgs> OnLayerSwitched;  // 已切换完成通知
    // 玩家移动相关
    public event EventHandler<PlayerArrivedEventArgs> OnPlayerArrived; // 玩家到达目标位置
    //public event EventHandler<PreMoveEventArgs> OnPreMoveRequested; // 预移动请求
    //public event EventHandler<PostMoveEventArgs> OnPostMoveArrived; // 到达后通知
    //网格事件相关
    public event EventHandler<GridEventTriggerEventArgs> OnGridEventTrigger; // 网格事件触发
    //战斗相关
    public event EventHandler<BattleCheckEventArgs> OnBattleCheckRequest; // 战斗检查请求
    public event EventHandler<EntityAnimationEventArgs> OnEntityAnimationFinished; // 实体动画完成通知


    public void TriggerLayerSwitched(LayerSwitchedEventArgs args) => OnLayerSwitched?.Invoke(this, args);
    public void TriggerPlayerArrived(PlayerArrivedEventArgs args) => OnPlayerArrived?.Invoke(this, args);

    //public void TriggerPreMoveRequest(PreMoveEventArgs args) => OnPreMoveRequested?.Invoke(this, args);
    //public void TriggerPostMoveArrived(PostMoveEventArgs args) => OnPostMoveArrived?.Invoke(this, args);

    public void TriggerGridEvent(GridEventTriggerEventArgs args) => OnGridEventTrigger?.Invoke(this, args);
    // 触发战斗检查请求
    public void TriggerBattleCheckRequest(BattleCheckEventArgs args) => OnBattleCheckRequest?.Invoke(this, args);

    // 新：触发实体动画完成事件
    public void TriggerEntityAnimationFinished(EntityAnimationEventArgs args) => OnEntityAnimationFinished?.Invoke(this, args);

}