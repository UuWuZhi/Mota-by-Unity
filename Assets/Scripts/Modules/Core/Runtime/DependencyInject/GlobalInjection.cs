using Modules.Core.DataDefine;
using Modules.Core.Runtime.GlobalVariables;
using Modules.Enemy.DataDefine;
using Modules.UI.Runtime;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace Modules.Core.Runtime.DependencyInject
{
    public class GlobalInjection : LifetimeScope
    {
        [FormerlySerializedAs("_itemDatabase")] [SerializeField]
        private ItemDatabase itemDatabase;

        [FormerlySerializedAs("_enemyDatabase")] [SerializeField]
        private EnemyDatabase enemyDatabase;

        protected override void Configure(IContainerBuilder builder)
        {
            // Monobehaviour 组件注册
            builder.RegisterComponentInHierarchy<EventCenter>().AsSelf();
            builder.RegisterComponentInHierarchy<UIManager>().AsSelf();

            // Interface 服务注册
            builder.Register<IGlobalEventVariables, GlobalEventVariablesService>(Lifetime.Singleton).AsSelf();

            // ScriptableObject 注册
            builder.RegisterInstance(itemDatabase).AsSelf();
            builder.RegisterInstance(enemyDatabase).AsSelf();
        }
    }
}