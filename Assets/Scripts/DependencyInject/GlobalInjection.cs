using VContainer;
using VContainer.Unity;
using UnityEngine;

public class GlobalInjection : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // EventCenter
        builder.RegisterComponentOnNewGameObject<EventCenter>(Lifetime.Singleton, "GlobalScripts")
            .DontDestroyOnLoad()
            .AsSelf();
    }
}  