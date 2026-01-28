using UnityEngine;
using VContainer;

/// <summary>
/// 全局服务容器：通过 DI 注入常用全局服务，便于在不使用静态 Instance 的地方访问。
/// </summary>
public class GlobalServiceContainer
{
    public EventCenter EventCenter { get; private set; }
    public MapManager MapManager { get; private set; }
    public PlayerAttribute PlayerAttribute { get; private set; }
    public IInventoryService InventoryService { get; private set; }
    public GridManager GridManager { get; private set; }
    public GoldRewardCaculate GoldRewardCaculate { get; private set; }

    [Inject]
    public void Construct(EventCenter eventCenter, MapManager mapManager, PlayerAttribute playerAttribute, IInventoryService inventoryService, GridManager gridManager, GoldRewardCaculate goldRewardCaculate)
    {
        EventCenter = eventCenter;
        MapManager = mapManager;
        PlayerAttribute = playerAttribute;
        InventoryService = inventoryService;
        GridManager = gridManager;
        GoldRewardCaculate = goldRewardCaculate;
        // 可在此处执行需要在容器可用时初始化的逻辑
        Debug.Log("GlobalServiceContainer constructed: services injected.");
    }
}
