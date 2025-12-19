using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SwitchLayerAction", menuName = "EventNodes/Action/SwitchLayer")]
public class SwitchLayerAction : ActionNode
{
    public StairType stairType;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        MapManager.Instance.GetLayerAndSpawnPosIDbyStairType(stairType, out int targetLayerId, out int spawnPointId);
        ctx.EventCenter.TriggerLayerSwitchRequest(new LayerSwitchRequestEventArgs
        {
            TargetLayerId = targetLayerId,
            SpawnPointId = spawnPointId
        });
        onComplete?.Invoke();
    }
}