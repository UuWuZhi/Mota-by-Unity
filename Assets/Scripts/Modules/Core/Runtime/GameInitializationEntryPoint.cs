using Modules.EventSystem.DataDefine.EventArgs;
using Modules.Map.Runtime;
using Modules.Player.DataDefine;
using Modules.Player.Runtime.Attribute;
using VContainer.Unity;

namespace Modules.Core.Runtime
{
    /// <summary>
    ///     表示游戏初始化的容器入口点。在容器构建并启动入口点时执行启动流程：加载全局地图、重置玩家属性、初始化背包道具，并请求层切换。
    /// </summary>
    /// <remarks>
    ///     在容器启动阶段由 IStartable.Start 调用。通过构造函数注入 IInventoryService、MapManager 和
    ///     PlayerAttribute。初始化顺序为：GlobalMapLoad → ResetAttribute → InitItemCounts → RequestLayerSwitch（默认目标
    ///     LayerId=0、SpawnPointId=0）。若需要玩家的 GameObject 引用，请在小型 MonoBehaviour 上保留检视器分配的引用并在运行时注册。
    /// </remarks>
    public class GameInitializationEntryPoint : IStartable
    {
        private readonly IInventoryService _inventoryService;
        private readonly MapManager _mapManager;
        private readonly PlayerAttribute _playerAttribute;

        public GameInitializationEntryPoint(IInventoryService inventoryService, MapManager mapManager,
            PlayerAttribute playerAttribute)
        {
            _inventoryService = inventoryService;
            _mapManager = mapManager;
            _playerAttribute = playerAttribute;
        }

        public void Start()
        {
            // 1. 先加载地图数据
            _mapManager.GlobalMapLoad();

            // 2. 初始化玩家属性
            _playerAttribute.ResetAttribute();

            // 3. 初始化背包道具
            _inventoryService.InitItemCounts();
            _mapManager.RequestLayerSwitch(new LayerSwitchRequestEventArgs
            {
                TargetLayerId = 0,
                SpawnPointId = 0
            });
        }
    }
}