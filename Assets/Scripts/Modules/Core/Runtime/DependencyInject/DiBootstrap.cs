using Modules.Core.Runtime.Calculate;
using Modules.Enemy.Runtime;
using Modules.EventNodeSystem.DataDefine.Runner;
using Modules.EventNodeSystem.Runtime;
using Modules.EventNodeSystem.Runtime.Runner;
using Modules.Item.DataDefine;
using Modules.Item.Runtime;
using Modules.Item.Runtime.MonsterBook;
using Modules.Map.DataDefine;
using Modules.Map.Runtime;
using Modules.Map.Runtime.EventTile;
using Modules.Player.DataDefine;
using Modules.Player.Runtime;
using Modules.Player.Runtime.Attribute;
using Modules.Player.Runtime.Inventory.UI;
using Modules.Player.Runtime.Movement;
using VContainer;
using VContainer.Unity;
using Yarn.Unity;

namespace Modules.Core.Runtime.DependencyInject
{
    /// <summary>
    ///     最小 Composition Root：把现有的单例实例注册到 VContainer 中，支持逐步迁移到 DI。
    ///     把此脚本挂到场景中的一个根 GameObject（保证在其他对象访问 Instance 之后执行）。
    ///     现在增加：可在 Inspector 指定一组 UI Prefab，容器构建后会自动 Instantiate + Inject，并注册到 UIManager。
    /// </summary>
    public class DiBootstrap : LifetimeScope
    {
        /// <summary>
        ///     注册全局服务与场景组件。
        /// </summary>
        /// <param name="builder">容器构建器。</param>
        protected override void Configure(IContainerBuilder builder)
        {
            // 地图相关
            builder.RegisterComponentInHierarchy<GridManager>().AsSelf();
            builder.RegisterComponentInHierarchy<MapManager>().AsSelf();

            // Yarn 注册
            var dialogueRunner = FindObjectOfType<DialogueRunner>();
            if (dialogueRunner)
            {
                builder.RegisterInstance(dialogueRunner);
                builder.Register<YarnRouteBridge>(Lifetime.Singleton).AsSelf();
            }

            // 事件格子注册表与管理器
            builder.Register<EventTileRegistry>(Lifetime.Singleton).As<IEventTileRegistry>()
                .AsSelf(); // Register EventTileRegistry
            builder.RegisterComponentInHierarchy<EventTileManager>().AsSelf(); // Register EventTileManager
            builder.RegisterComponentInHierarchy<CoroutineRunner>().AsSelf();
            // ENS 注册表：用于 Data 类型到节点模板的映射
            builder.Register<EventNodeSystemRegistry>(Lifetime.Singleton).AsSelf();

            // 玩家相关
            builder.RegisterComponentInHierarchy<PlayerAttribute>().AsSelf();
            builder.RegisterComponentInHierarchy<PlayerState>().AsSelf();
            builder.RegisterComponentInHierarchy<PlayerMovement>().AsSelf();
            builder.RegisterComponentInHierarchy<MovementInputManager>().AsSelf();
            builder.RegisterComponentInHierarchy<PlayerInventory>().AsSelf().As<IInventoryService>();
            // UI slots in scene should be injected as components in hierarchy so their [Inject] methods run
            builder.RegisterComponentInHierarchy<InventorySlot>().AsSelf();

            // UI相关
            // ... (UIDialogue已移除，改用Yarn Spinner对话系统) ...

            // 服务类
            builder.Register<GoldRewardCalculate>(Lifetime.Transient).AsSelf();
            // ... (DialogueManager已移除，暂不注入，由Yarn接管) ...
            // if (BattleManager.Instance)
            //     builder.RegisterInstance(BattleManager.Instance).As<BattleManager>().AsSelf();
            builder.RegisterComponentInHierarchy<BattleManager>().AsSelf();
            builder.Register<IMonsterBook, MonsterBookService>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<GameInitializationEntryPoint>();

            builder.Register<EventRunnerService>(Lifetime.Singleton).As<IEventRunner>().AsSelf();
            // Register ItemUseHandler to allow UI and other systems to use items via a unified handler
            builder.Register<ItemUseHandler>(Lifetime.Singleton).AsSelf();
        }
    }
}