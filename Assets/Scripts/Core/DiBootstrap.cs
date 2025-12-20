using UnityEngine;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-200)]
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
        if (DialogueManager.Instance != null) builder.RegisterInstance(DialogueManager.Instance).As<DialogueManager>().AsSelf();
        if (PlayerAttribute.Instance != null) builder.RegisterInstance(PlayerAttribute.Instance).As<PlayerAttribute>().AsSelf();
        if (PlayerMovement.Instance != null) builder.RegisterInstance(PlayerMovement.Instance).As<PlayerMovement>().AsSelf();

        // 如果场景中存在 PlayerInventory，则把它注册到容器，避免构造器解析失败
        //if (PlayerInventory.Instance != null)
        //{
        //    builder.RegisterInstance(PlayerInventory.Instance).As<PlayerInventory>().AsSelf();
        //}

        // 注册 InventoryAdapter 由容器管理；容器会尝试使用无参构造或通过构造参数注入
        builder.Register<InventoryAdapter>(Lifetime.Singleton).As<IInventoryService>().AsSelf();

        // 注册 GameInitializationEntryPoint（容器负责构造并在启动时调用 Start）
        builder.RegisterEntryPoint<GameInitializationEntryPoint>();

        // 注册 EntryPoint 用于演示容器注入（同时保证 InventoryAdapter 被构造）
        builder.RegisterEntryPoint<InventoryLogger>();

        // 示例：注册一个纯 C# 服务为单例
        // builder.Register<YourService>(Lifetime.Singleton);
    }
}