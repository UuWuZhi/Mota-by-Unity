using UnityEngine;
using VContainer.Unity;
using VContainer;
using System.Collections.Generic;

/// <summary>
/// Container-managed entrypoint to perform game initialization using injected services.
/// Replaces GameInitializer's runtime dependence on global InventoryAdapter.Current.
/// </summary>
public class GameInitializationEntryPoint : IStartable
{
    private readonly IInventoryService _inventoryService;
    private readonly MapManager _mapManager;
    private readonly PlayerAttribute _playerAttribute;
    private readonly EventCenter _eventCenter;

    // If you need the player GameObject reference, keep an inspector-assigned reference on a small MonoBehaviour and register its instance.
    public GameInitializationEntryPoint(IInventoryService inventoryService, MapManager mapManager, PlayerAttribute playerAttribute, EventCenter eventCenter)
    {
        _inventoryService = inventoryService;
        _mapManager = mapManager;
        _playerAttribute = playerAttribute;
        _eventCenter = eventCenter;
    }

    // IStartable.Start runs when the container builds and starts entrypoints
    public void Start()
    {
        // 1. 先加载地图数据
        _mapManager.GlobalMapLoad();
        Debug.Log("地图数据加载完成！（Container EntryPoint）");

        // 2. 初始化玩家属性
        _playerAttribute.ResetAttribute();
        Debug.Log("玩家属性初始化完成！（Container EntryPoint）");

        // 3. 初始化背包道具
        _inventoryService.InitItemCounts();
        Debug.Log("背包道具初始化完成！（Container EntryPoint）");
        _eventCenter.TriggerLayerSwitchRequest(new LayerSwitchRequestEventArgs
        {
            TargetLayerId = 1,
            SpawnPointId = 0
        });
    }
}
