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
    [SerializeField] private PlayerAttribute _playerAttribute;
    [SerializeField] private PlayerMovement _playerMovement;
    [SerializeField] private UIDialogue _uiDialogue;

    protected override void Configure(IContainerBuilder builder)
    {
        //if (EventNodeManager.Instance != null) builder.RegisterInstance(EventNodeManager.Instance).As<EventNodeManager>().AsSelf();
        if (EventCenter.Instance != null) builder.RegisterInstance(EventCenter.Instance).As<EventCenter>().AsSelf();

        builder.Register<DialogueManager>(Lifetime.Singleton).AsSelf();

        // Register GlobalEventVariablesService as the IGlobalEventVariables implementation
        builder.Register<GlobalEventVariablesService>(Lifetime.Singleton).As<IGlobalEventVariables>().AsSelf();
        builder.Register<InventoryAdapter>(Lifetime.Singleton).As<IInventoryService>().AsSelf();
        builder.Register<GoldRewardCaculate>(Lifetime.Singleton).AsSelf();

        builder.RegisterComponentInHierarchy<MapManager>().AsSelf();
        builder.RegisterComponentInHierarchy<GridManager>().AsSelf();
        builder.RegisterComponentInHierarchy<EventNodeManager>().AsSelf();
        builder.RegisterComponent(_playerAttribute).AsSelf();
        builder.RegisterComponent(_playerMovement).AsSelf();
        builder.RegisterComponent(_uiDialogue).AsSelf();
        //builder.RegisterComponentInHierarchy<PlayerInventory>().AsSelf(); 我们暂时不使用这种方式注入 PlayerInventory
        builder.Register<GlobalServiceContainer>(Lifetime.Singleton).AsSelf().As<GlobalServiceContainer>();
        // 如果场景中存在 GlobalEventVariables Mono，注入其 service
        var gev = GameObject.FindObjectOfType<GlobalEventVariables>();
        if (gev != null)
        {
            // InjectGameObject will cause its [Inject] Construct to be called after container build
            // we register a build callback to inject the scene object
            builder.RegisterBuildCallback(container => container.InjectGameObject(gev.gameObject));
        }
        
        if (UIManager.Instance != null) builder.RegisterInstance(UIManager.Instance).As<UIManager>().AsSelf();
        if (BattleManager.Instance != null) builder.RegisterInstance(BattleManager.Instance).As<BattleManager>().AsSelf();

        builder.RegisterEntryPoint<GameInitializationEntryPoint>();
        builder.RegisterEntryPoint<InventoryLogger>();

        // 在容器构建完成后对场景中的现有实例与动态实例执行注入
        builder.RegisterBuildCallback(container =>
        {
            // 注入单例实例（使其 [Inject] 方法/属性 被调用）
            //if (EventNodeManager.Instance != null) container.Inject(EventNodeManager.Instance);
            if (EventCenter.Instance != null) container.Inject(EventCenter.Instance);
            if (UIManager.Instance != null) container.Inject(UIManager.Instance);
            // DialogueManager is container-created, no scene Instance to inject

            // 注入场景中所有 PlayerInventory 组件（替代原先的 Instance 注入方式）
            foreach (var inv in GameObject.FindObjectsOfType<PlayerInventory>())
            {
                if (inv != null && inv.gameObject != null)
                    container.InjectGameObject(inv.gameObject);
            }

            // 自动注入所有场景中的 ItemUIManager / UIItem 组件（确保它们的 [Inject] 被调用）
            foreach (var ui in GameObject.FindObjectsOfType<UIItem>())
            {
                if (ui != null && ui.gameObject != null)
                    container.InjectGameObject(ui.gameObject);
            }

            // 注入所有已存在的输入管理组件（如果场景中挂载了这些 MonoBehaviour）
            foreach (var im in GameObject.FindObjectsOfType<UIInputManager>())
            {
                if (im != null && im.gameObject != null)
                    container.InjectGameObject(im.gameObject);
            }
            foreach (var im in GameObject.FindObjectsOfType<MovementInputManager>())
            {
                if (im != null && im.gameObject != null)
                    container.InjectGameObject(im.gameObject);
            }
            foreach (var im in GameObject.FindObjectsOfType<DialogueInputManager>())
            {
                if (im != null && im.gameObject != null)
                    container.InjectGameObject(im.gameObject);
            }

            // 注入常见 UI 组件，确保它们的 [Inject] 被调用
            foreach (var ui in GameObject.FindObjectsOfType<UIDialogue>())
            {
                if (ui != null && ui.gameObject != null)
                    container.InjectGameObject(ui.gameObject);
            }
            foreach (var ui in GameObject.FindObjectsOfType<MonsterBookUI>())
            {
                if (ui != null && ui.gameObject != null)
                    container.InjectGameObject(ui.gameObject);
            }
            foreach (var ui in GameObject.FindObjectsOfType<MonsterBar>())
            {
                if (ui != null && ui.gameObject != null)
                    container.InjectGameObject(ui.gameObject);
            }
            foreach (var ui in GameObject.FindObjectsOfType<UIAttribute>())
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