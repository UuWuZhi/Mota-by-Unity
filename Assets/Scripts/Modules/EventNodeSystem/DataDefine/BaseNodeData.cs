using System;

namespace Modules.EventNodeSystem.DataDefine
{
    [Serializable]
    public abstract class BaseNodeData
    {
        public abstract string GetSummary();
    }
}