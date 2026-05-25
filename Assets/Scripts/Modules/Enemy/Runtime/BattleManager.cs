using System;
using Modules.Core.Runtime;
using Modules.EventSystem.DataDefine.EventArgs;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Modules.Enemy.Runtime
{
    /// <summary>
    ///     战斗数据容器（玩家/敌人共用的战斗属性）
    /// </summary>
    [Serializable]
    public class BattleUnitData
    {
        [FormerlySerializedAs("currentHP")] [Tooltip("当前生命值")]
        public int currentHp;

        [Tooltip("攻击力")] public int attack;

        [Tooltip("防御力")] public int defense;
    }

    /// <summary>
    ///     战斗结果类型
    /// </summary>
    public enum BattleResult
    {
        None, // 未结束
        PlayerWin, // 玩家胜利
        PlayerLose // 玩家失败
    }

    /// <summary>
    ///     魔塔回合制战斗管理器
    ///     核心逻辑：玩家先攻，双方轮流攻击，伤害=攻击者攻击力-被攻击者防御力（最低1点伤害）
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        private EventCenter _eventCenter;

        // 单例实例（方便全局调用）
        public static BattleManager Instance { get; private set; }

        private void Awake()
        {
            if (!Instance)
                Instance = this;
            else
                Destroy(gameObject);
        }

        private void OnDisable()
        {
            // 取消订阅
            _eventCenter.OnBattleCheckRequest -= OnBattleCheckRequest;
        }

        [Inject]
        public void Inject(EventCenter eventCenter)
        {
            _eventCenter = eventCenter;
            _eventCenter.OnBattleCheckRequest += OnBattleCheckRequest;
        }

        /// <summary>
        ///     处理战斗可行性检查请求（复用 ResolveBattle 做计算）
        /// </summary>
        private void OnBattleCheckRequest(object sender, BattleCheckEventArgs args)
        {
            DebugEditor.Log("OnBattleCheckRequest:接收到查询请求！");
            // 使用 ResolveBattle 模拟，不修改原始传入数据
            var playerCopy = new BattleUnitData
            {
                currentHp = args.PlayerData.currentHp,
                attack = args.PlayerData.attack,
                defense = args.PlayerData.defense
            };
            var enemyCopy = new BattleUnitData
            {
                currentHp = args.EnemyData.currentHp,
                attack = args.EnemyData.attack,
                defense = args.EnemyData.defense
            };

            var result = ResolveBattle(playerCopy, enemyCopy, out var hpLoss);

            if (result == BattleResult.PlayerWin)
            {
                args.TotalPlayerHpLoss = hpLoss;
                args.IsDefeatable = true;
            }
            else
            {
                // 只有在“无法造成伤害”（敌人防御>=玩家攻击）时标记为不可战斗并返回 int.MaxValue
                // 其他失败情况仍然返回实际消耗的 HP（可能大于玩家当前 HP）以便 UI 显示预期损伤
                if (playerCopy.attack <= enemyCopy.defense)
                {
                    args.TotalPlayerHpLoss = int.MaxValue; // 无法打败时返回最大值
                    args.IsDefeatable = false;
                }
                else
                {
                    args.TotalPlayerHpLoss = hpLoss;
                    args.IsDefeatable = false;
                }
            }
        }

        /// <summary>
        ///     对外的战斗解析接口：传入玩家与敌人的战斗数据（会在内部复制以避免修改外部实例）
        ///     返回战斗结果，并通过 out 参数返回玩家战斗中消耗的总 HP（若失败且无法造成伤害返回 int.MaxValue）
        /// </summary>
        public BattleResult ResolveBattle(BattleUnitData playerData, BattleUnitData enemyData,
            out int totalPlayerHpLoss)
        {
            // 复制输入，保证外部对象不被修改
            var player = new BattleUnitData
            {
                currentHp = playerData.currentHp,
                attack = playerData.attack,
                defense = playerData.defense
            };
            var enemy = new BattleUnitData
            {
                currentHp = enemyData.currentHp,
                attack = enemyData.attack,
                defense = enemyData.defense
            };

            // 如果玩家对敌人无法造成任何伤害，则视为无法战斗（直接返回失败并用 int.MaxValue 标记）
            if (player.attack <= enemy.defense)
            {
                totalPlayerHpLoss = int.MaxValue;
                return BattleResult.PlayerLose;
            }

            var initialPlayerHp = player.currentHp;
            var isPlayerWin = SimulateBattle(player, enemy);

            // 无论胜负都返回实际消耗的HP（若失败也返回消耗值，而非 int.MaxValue），上层可以根据 IsDefeatable 判断是否可通过
            totalPlayerHpLoss = initialPlayerHp - player.currentHp;
            return isPlayerWin ? BattleResult.PlayerWin : BattleResult.PlayerLose;
        }

        /// <summary>
        ///     模拟回合制战斗过程（内部使用的计算逻辑）
        /// </summary>
        private bool SimulateBattle(BattleUnitData player, BattleUnitData enemy)
        {
            // 战斗循环：玩家先攻，轮流攻击直到一方死亡
            while (true)
            {
                // 玩家攻击（确保玩家能造成伤害时才会进入此方法）
                var playerDamage = Mathf.Max(player.attack - enemy.defense, 1);
                enemy.currentHp -= playerDamage;
                if (enemy.currentHp <= 0)
                    return true; // 玩家胜利

                // 敌人反击
                var enemyDamage = Mathf.Max(enemy.attack - player.defense, 1);
                player.currentHp -= enemyDamage;
                if (player.currentHp <= 0)
                    return false; // 玩家失败
            }
        }
    }
}