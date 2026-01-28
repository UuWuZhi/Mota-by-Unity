using VContainer;
using VContainer.Unity;
using UnityEngine;

public class GlobalInjection : LifetimeScope
{
    [SerializeField] private ItemDatabase _itemDatabase;
    [SerializeField] private EnemyDatabase _enemyDatabase;
    protected override void Configure(IContainerBuilder builder)
    {
        // Monobehaviour 组件注册
        builder.RegisterComponentInHierarchy<EventCenter>().AsSelf();
        builder.RegisterComponentInHierarchy<UIManager>().AsSelf();

        // Interface 服务注册
        builder.Register<IGlobalEventVariables, GlobalEventVariablesService>(Lifetime.Singleton).AsSelf();

        // ScriptableObject 注册
        builder.RegisterInstance(_itemDatabase).AsSelf();
        builder.RegisterInstance(_enemyDatabase).AsSelf();
        
    }
}  