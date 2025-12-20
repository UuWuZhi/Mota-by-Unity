using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 最小 Composition Root：把现有的单例实例注册到 VContainer 中，支持逐步迁移到 DI。
/// 把此脚本挂到场景中的一个根 GameObject（保证在其他对象访问 Instance 之后执行）。
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
        if (PlayerInventory.Instance != null) builder.RegisterInstance(PlayerInventory.Instance).As<PlayerInventory>().AsSelf();
        if (DialogueManager.Instance != null) builder.RegisterInstance(DialogueManager.Instance).As<DialogueManager>().AsSelf();

        // 注册适配器：把 PlayerInventory 暴露为 IInventoryService
        builder.Register<InventoryAdapter>(Lifetime.Singleton).As<IInventoryService>();

        // 注册 EntryPoint 用于演示容器注入
        builder.RegisterEntryPoint<InventoryLogger>();

        // 示例：注册一个纯 C# 服务为单例
        // builder.Register<YourService>(Lifetime.Singleton);
    }
}