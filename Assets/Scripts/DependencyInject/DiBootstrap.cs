using System.Collections.Generic;
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
        builder.RegisterComponentInHierarchy<EventTileManager>().AsSelf();
        builder.RegisterComponentInHierarchy<CoroutineRunner>().AsSelf();
        builder.Register<EventRunnerService>(Lifetime.Singleton).As<IEventRunner>().AsSelf();

        // 玩家相关
        builder.RegisterComponentInHierarchy<PlayerAttribute>().AsSelf();
        builder.RegisterComponentInHierarchy<PlayerState>().AsSelf();
        builder.RegisterComponentInHierarchy<PlayerMovement>().AsSelf();
        builder.RegisterComponentInHierarchy<MovementInputManager>().AsSelf();
        builder.RegisterComponentInHierarchy<PlayerInventory>().AsSelf().As<IInventoryService>();

        // UI相关
        builder.RegisterComponentInHierarchy<UIDialogue>().AsSelf();

        // 服务类
        builder.Register<GoldRewardCaculate>(Lifetime.Transient).AsSelf();
        builder.Register<DialogueManager>(Lifetime.Singleton).AsSelf();
        if (BattleManager.Instance != null) builder.RegisterInstance(BattleManager.Instance).As<BattleManager>().AsSelf();
        builder.Register<IMonsterBook, MonsterBookService>(Lifetime.Singleton).AsSelf();
        builder.RegisterEntryPoint<GameInitializationEntryPoint>();

        // 在容器构建完成后对场景中的现有实例与动态实例执行注入
        builder.RegisterBuildCallback(container =>
        {

            foreach (var ui in GameObject.FindObjectsOfType<MonsterBar>())
            {
                if (ui != null && ui.gameObject != null)
                    container.InjectGameObject(ui.gameObject);
            }

            // 也尝试注入 EventNodeTile owners if needed (inject their components)
            foreach (var tile in GameObject.FindObjectsOfType<EventNodeTile>())
            {
                if (tile != null && tile.gameObject != null)
                    container.InjectGameObject(tile.gameObject);
            }
        });
    }
}