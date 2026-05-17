using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Modules.EventNodeSystem.DataDefine
{
    [Serializable]
    public class EventSequence
    {
        [FormerlySerializedAs("Commands")] [SerializeReference]
        public List<BaseNodeData> commands = new();
    }
}