//全局事件管理
using UnityEngine;
using UnityEngine.Tilemaps;
using System;

// 全局事件中心（单例，统一管理所有游戏事件）
public class EventCenter : MonoBehaviour
{
    public static EventCenter Instance;
    // 层数切换相关
    public event EventHandler<LayerSwitchedEventArgs> OnLayerSwitched;  // 已切换完成通知
    public event EventHandler<LayerSwitchRequestEventArgs> OnLayerSwitchRequested; // 切换请求
    public event EventHandler<GridLoadedEventArgs> OnGridLoaded; // 地图格子加载完成通知
    // 玩家移动相关
    public event EventHandler<PlayerInputEventArgs> OnPlayerMoveInput; // 玩家移动输入
    public event EventHandler<PlayerMoveDirectionChangedEventArgs> OnMoveDirectionChanged; // 移动方向改变
    public event EventHandler<PlayerMoveStateChangedEventArgs> OnMoveStateChanged; // 移动状态改变
    public event EventHandler<PlayerArrivedEventArgs> OnPlayerArrived; // 玩家到达目标位置
    //public event EventHandler<PreMoveEventArgs> OnPreMoveRequested; // 预移动请求
    //public event EventHandler<PostMoveEventArgs> OnPostMoveArrived; // 到达后通知
    //网格事件相关
    public event EventHandler<GridEventTriggerEventArgs> OnGridEventTrigger; // 网格事件触发
    public event EventHandler<TileMovedEventArgs> OnEventTileMoved; // 事件层瓦片移动通知
    //战斗相关
    public event EventHandler<BattleCheckEventArgs> OnBattleCheckRequest; // 战斗检查请求
    public event EventHandler<AttributeChangedEventArgs> OnAttributeChanged; // 属性变化通知
    public event EventHandler<EntityAnimationEventArgs> OnEntityAnimationFinished; // 实体动画完成通知

    // UI 相关事件：用于控制 UI 的显示/隐藏
    public event EventHandler<UIShowEventArgs> OnShowUI; // 请求显示特定 UI
    public event EventHandler<UIHideEventArgs> OnHideUI; // 请求隐藏特定 UI
    public event EventHandler<UIToggleEventArgs> OnToggleUI; // 请求切换特定 UI 显示状态

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        DontDestroyOnLoad(gameObject); // 全局保留
    }

    public void TriggerLayerSwitched(LayerSwitchedEventArgs args) => OnLayerSwitched?.Invoke(this, args);
    public void TriggerLayerSwitchRequest(LayerSwitchRequestEventArgs args) => OnLayerSwitchRequested?.Invoke(this, args);
    public void TriggerPlayerMoveInput(PlayerInputEventArgs args) => OnPlayerMoveInput?.Invoke(this, args);
    public void TriggerMoveDirectionChanged(PlayerMoveDirectionChangedEventArgs args) => OnMoveDirectionChanged?.Invoke(this, args);
    public void TriggerMoveStateChanged(PlayerMoveStateChangedEventArgs args) => OnMoveStateChanged?.Invoke(this, args);
    public void TriggerPlayerArrived(PlayerArrivedEventArgs args) => OnPlayerArrived?.Invoke(this, args);

    //public void TriggerPreMoveRequest(PreMoveEventArgs args) => OnPreMoveRequested?.Invoke(this, args);
    //public void TriggerPostMoveArrived(PostMoveEventArgs args) => OnPostMoveArrived?.Invoke(this, args);

    public void TriggerGridEvent(GridEventTriggerEventArgs args) => OnGridEventTrigger?.Invoke(this, args);
    // 触发战斗检查请求
    public void TriggerBattleCheckRequest(BattleCheckEventArgs args) => OnBattleCheckRequest?.Invoke(this, args);
    public void TriggerAttributeChanged(AttributeChangedEventArgs args) => OnAttributeChanged?.Invoke(this, args);
    public void TriggerGridLoaded(GridLoadedEventArgs args) => OnGridLoaded?.Invoke(this, args);

    // 新：触发实体动画完成事件
    public void TriggerEntityAnimationFinished(EntityAnimationEventArgs args) => OnEntityAnimationFinished?.Invoke(this, args);

    // 新：触发事件层瓦片移动事件
    public void TriggerEventTileMoved(TileMovedEventArgs args) => OnEventTileMoved?.Invoke(this, args);

    // UI 触发方法
    public void TriggerShowUI(UIShowEventArgs args) => OnShowUI?.Invoke(this, args);
    public void TriggerHideUI(UIHideEventArgs args) => OnHideUI?.Invoke(this, args);
    public void TriggerToggleUI(UIToggleEventArgs args) => OnToggleUI?.Invoke(this, args);
}