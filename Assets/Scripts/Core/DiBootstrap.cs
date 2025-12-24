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
        // 将现有的单例实例注册到容器中，便于新写类通过构造注入或属性注入使用它们。
        // 使用 RegisterInstance 可避免一开始就重构现有类的 Awake/Start 顺序。
        if (EventNodeManager.Instance != null) builder.RegisterInstance(EventNodeManager.Instance).As<EventNodeManager>().AsSelf();
        if (EventCenter.Instance != null) builder.RegisterInstance(EventCenter.Instance).As<EventCenter>().AsSelf();
        if (GridManager.Instance != null) builder.RegisterInstance(GridManager.Instance).As<GridManager>().AsSelf();
        if (MapManager.Instance != null) builder.RegisterInstance(MapManager.Instance).As<MapManager>().AsSelf();
        if (DialogueManager.Instance != null) builder.RegisterInstance(DialogueManager.Instance).As<DialogueManager>().AsSelf();
        if (PlayerAttribute.Instance != null) builder.RegisterInstance(PlayerAttribute.Instance).As<PlayerAttribute>().AsSelf();
        if (PlayerMovement.Instance != null) builder.RegisterInstance(PlayerMovement.Instance).As<PlayerMovement>().AsSelf();
        if (PlayerInventory.Instance != null) builder.RegisterInstance(PlayerInventory.Instance).As<PlayerInventory>().AsSelf();
        if (UIManager.Instance != null) builder.RegisterInstance(UIManager.Instance).As<UIManager>().AsSelf();
        // 注册 InventoryAdapter 由容器管理；容器会尝试使用无参构造或通过构造参数注入
        builder.Register<InventoryAdapter>(Lifetime.Singleton).As<IInventoryService>().AsSelf();


        builder.RegisterEntryPoint<GameInitializationEntryPoint>();
        builder.RegisterEntryPoint<InventoryLogger>();

        // 在容器构建完成后对场景中的现有实例与动态实例执行注入
        builder.RegisterBuildCallback(container =>
        {
            // 注入单例实例（使其 [Inject] 方法/属性 被调用）
            if (EventNodeManager.Instance != null) container.Inject(EventNodeManager.Instance);
            if (EventCenter.Instance != null) container.Inject(EventCenter.Instance);
            if (PlayerInventory.Instance != null) container.Inject(PlayerInventory.Instance);
            if (UIManager.Instance != null) container.Inject(UIManager.Instance);
            if (DialogueManager.Instance != null) container.Inject(DialogueManager.Instance);

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

            // 也尝试注入 EventNodeTile owners if needed (inject their components)
            foreach (var tile in GameObject.FindObjectsOfType<EventNodeTile>())
            {
                if (tile != null && tile.gameObject != null)
                    container.InjectGameObject(tile.gameObject);
            }
        });
    }
}