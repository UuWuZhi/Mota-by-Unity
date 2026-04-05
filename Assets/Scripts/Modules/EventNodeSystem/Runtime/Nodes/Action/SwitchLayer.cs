using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SwitchLayer", menuName = "EventNodes/Action/SwitchLayer")]
public class SwitchLayer : ActionNode
{
    public StairType stairType;

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(MapManager) };
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        var mapManager = ctx?.GetService<MapManager>();

        if (mapManager == null)
        {
            Debug.LogError("SwitchLayer: MapManager 未配置，无法切换楼层。");
            onComplete?.Invoke();
            return;
        }

        mapManager.GetLayerAndSpawnPosIDbyStairType(stairType, out int targetLayerId, out int spawnPointId);
        mapManager.RequestLayerSwitch(new LayerSwitchRequestEventArgs
        {
            TargetLayerId = targetLayerId,
            SpawnPointId = spawnPointId
        });
        onComplete?.Invoke();
    }
}