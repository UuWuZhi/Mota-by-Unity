using System;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.DataDefine.Context;
using Modules.EventNodeSystem.Runtime.Nodes.Action.Data;
using Modules.EventSystem.DataDefine.EventArgs;
using Modules.Map.Runtime;
using UnityEngine;

namespace Modules.EventNodeSystem.Runtime.Nodes.Action
{
    [CreateAssetMenu(fileName = "SwitchLayer", menuName = "EventNodes/Action/SwitchLayer")]
    public class SwitchLayer : ActionNode
    {
        public override Type[] GetRequiredServices()
        {
            return new[] { typeof(MapManager) };
        }

        public override void Execute(BaseNodeData data, EventNodeContext ctx, System.Action onComplete)
        {
            var switchData = data as SwitchLayerData;
            if (switchData == null)
            {
                Debug.LogWarning("SwitchLayer: data 类型不匹配，跳过执行。");
                onComplete?.Invoke();
                return;
            }

            var mapManager = ctx?.GetService<MapManager>();

            if (mapManager == null)
            {
                Debug.LogError("SwitchLayer: MapManager 未配置，无法切换楼层。");
                onComplete?.Invoke();
                return;
            }

            mapManager.GetLayerAndSpawnPosIDbyStairType(switchData.stairType, out var targetLayerId,
                out var spawnPointId);
            mapManager.RequestLayerSwitch(new LayerSwitchRequestEventArgs
            {
                TargetLayerId = targetLayerId,
                SpawnPointId = spawnPointId
            });
            onComplete?.Invoke();
        }
    }
}