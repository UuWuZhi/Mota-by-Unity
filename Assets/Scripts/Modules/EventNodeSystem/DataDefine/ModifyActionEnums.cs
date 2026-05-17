namespace Modules.EventNodeSystem.DataDefine.Data
{
    /// <summary>
    ///     Modify 节点通用枚举定义
    /// </summary>
    public enum ModifyOperation
    {
        Add,
        Remove,
        Set
    }

    public enum ModifyParameterSource
    {
        Fixed,
        TileUnit,
        Vars
    }
}