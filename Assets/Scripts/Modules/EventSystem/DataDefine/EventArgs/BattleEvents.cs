// 新增文件：Scripts/EventSystem/EventArgs/BattleCheckEventArgs.cs
using System;

/// <summary>
/// 战斗可行性检查事件参数
/// </summary>
public class BattleCheckEventArgs : EventArgs
{
    // 输入参数：玩家和敌人数据
    public BattleUnitData PlayerData { get; set; }
    public BattleUnitData EnemyData { get; set; }

    // 输出参数：计算结果
    public int TotalPlayerHPLoss { get; set; } // 玩家战胜敌人的总HP消耗
    public bool IsDefeatable { get; set; }     // 是否可以打败敌人
}