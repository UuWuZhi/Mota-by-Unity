using VContainer;
using VContainer.Unity;
using UnityEngine;

public class GlobalInjection : LifetimeScope
{
    [SerializeField] private MapManager _mapManager;
    [SerializeField] private ItemDatabase _itemDatabase;
    protected override void Configure(IContainerBuilder builder)
    {
        // Monobehaviour 组件注册
        builder.RegisterComponentInHierarchy<EventCenter>().AsSelf();

        // Interface 服务注册
        builder.Register<IGlobalEventVariables, GlobalEventVariablesService>(Lifetime.Singleton).AsSelf();

        // ScriptableObject 注册
        builder.RegisterInstance(_itemDatabase).AsSelf();
    }
}  