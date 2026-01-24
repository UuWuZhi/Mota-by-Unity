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
    //[SerializeField] private PlayerAttribute _playerAttribute;
    //[SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private UIDialogue _uiDialogue;


    protected override void Configure(IContainerBuilder builder)
    {
        // 地图相关
        builder.RegisterComponentInHierarchy<GridManager>().AsSelf();
        builder.RegisterComponentInHierarchy<MapManager>().AsSelf();
        builder.RegisterComponentInHierarchy<EventNodeManager>().AsSelf();

        // 玩家相关
        builder.RegisterComponentInHierarchy<PlayerAttribute>().AsSelf();
        builder.RegisterComponentInHierarchy<PlayerMovement>().AsSelf();
        builder.RegisterComponentInHierarchy<MovementInputManager>().AsSelf();
        builder.Register<InventoryAdapter>(Lifetime.Singleton).As<IInventoryService>().AsSelf();

        builder.Register<DialogueManager>(Lifetime.Singleton).AsSelf();

        // 服务类
        builder.Register<GoldRewardCaculate>(Lifetime.Transient).AsSelf();

        
        builder.RegisterComponentInHierarchy<UIInputManager>().AsSelf();
        builder.RegisterComponent(_uiDialogue).AsSelf();

        builder.Register<GlobalServiceContainer>(Lifetime.Singleton).AsSelf().As<GlobalServiceContainer>();

        
        if (UIManager.Instance != null) builder.RegisterInstance(UIManager.Instance).As<UIManager>().AsSelf();
        if (BattleManager.Instance != null) builder.RegisterInstance(BattleManager.Instance).As<BattleManager>().AsSelf();

        builder.RegisterEntryPoint<GameInitializationEntryPoint>();
        builder.RegisterEntryPoint<InventoryLogger>();

        // 在容器构建完成后对场景中的现有实例与动态实例执行注入
        builder.RegisterBuildCallback(container =>
        {

            if (UIManager.Instance != null) container.Inject(UIManager.Instance);


            // 注入场景中所有 PlayerInventory 组件（替代原先的 Instance 注入方式）
            foreach (var inv in GameObject.FindObjectsOfType<PlayerInventory>())
            {
                if (inv != null && inv.gameObject != null)
                    container.InjectGameObject(inv.gameObject);
            }

            foreach (var im in GameObject.FindObjectsOfType<DialogueInputManager>())
            {
                if (im != null && im.gameObject != null)
                    container.InjectGameObject(im.gameObject);
            }
            foreach (var im in GameObject.FindObjectsOfType<BattleManager>())
            {
                if (im != null && im.gameObject != null)
                    container.InjectGameObject(im.gameObject);
            }


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