using UnityEngine;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-200)]
/// <summary>
/// 最小 Composition Root：把现有的单例实例注册到 VContainer 中，支持逐步迁移到 DI。
/// 把此脚本挂到场景中的一个根 GameObject（保证在其他对象访问 Instance 之后执行）。
/// 现在增加：可在 Inspector 指定一组 UI Prefab，容器构建后会自动 Instantiate + Inject，并注册到 UIManager。
/// </summary>
public class DiBootstrap : LifetimeScope
{

    protected override void Configure(IContainerBuilder builder)
    {
        // 地图相关
        builder.RegisterComponentInHierarchy<GridManager>().AsSelf();
        builder.RegisterComponentInHierarchy<MapManager>().AsSelf();

        // Yarn 注册
        var dialogueRunner = FindObjectOfType<Yarn.Unity.DialogueRunner>();
        if (dialogueRunner != null)
        {
            builder.RegisterInstance(dialogueRunner);
            builder.Register<YarnRouteBridge>(Lifetime.Singleton).AsSelf();
        }

        // Register registry implementation and EventTileManager separately. Keep manager registered as self.
        builder.Register<EventTileRegistry>(Lifetime.Singleton).As<IEventTileRegistry>().AsSelf(); // Register EventTileRegistry
        builder.RegisterComponentInHierarchy<EventTileManager>().AsSelf(); // Register EventTileManager
        builder.RegisterComponentInHierarchy<CoroutineRunner>().AsSelf();
        
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
        builder.Register<GoldRewardCaculate>(Lifetime.Transient).AsSelf();
        // ... (DialogueManager已移除，暂不注入，由Yarn接管) ...
        if (BattleManager.Instance != null) builder.RegisterInstance(BattleManager.Instance).As<BattleManager>().AsSelf();
        builder.Register<IMonsterBook, MonsterBookService>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<GameInitializationEntryPoint>();

        builder.Register<EventRunnerService>(Lifetime.Singleton).As<IEventRunner>().AsSelf();
        // Register ItemUseHandler to allow UI and other systems to use items via a unified handler
        builder.Register<ItemUseHandler>(Lifetime.Singleton).AsSelf();
    }
}