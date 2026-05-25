using Modules.Core.Runtime;
using Modules.EventSystem.DataDefine.EventArgs;
using Modules.Map.Runtime;
using Modules.Player.Runtime.Movement;
using UnityEngine;
using VContainer;

namespace Modules.Player.Runtime
{
    /// <summary>
    ///     玩家朝向枚举
    /// </summary>
    public enum Facing
    {
        Up,
        Down,
        Left,
        Right
    }


    /// <summary>
    ///     存储玩家的位置信息（格子坐标/世界坐标）、高度与朝向。
    ///     单一职责的状态组件，可通过 DI 注入或 GetComponent 获取。
    ///     坐标通过监听 PlayerArrived 事件更新，朝向通过监听 PlayerMoveInput 更新。
    /// </summary>
    public class PlayerState : MonoBehaviour
    {
        private EventCenter _eventCenter;
        private GridManager _gridManager;
        private PlayerMovement _playerMovement;
        private bool _subscribed;

        public Vector3Int CellPos { get; private set; }
        public Vector2 WorldPos { get; private set; }
        public int Height { get; private set; }
        public Facing Facing { get; private set; }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        [Inject]
        public void Inject(EventCenter eventCenter, PlayerMovement playerMovement, GridManager gridManager)
        {
            _eventCenter = eventCenter;
            _playerMovement = playerMovement;
            _gridManager = gridManager;
            Subscribe();
        }

        private void Subscribe()
        {
            if (!_eventCenter || _subscribed) return;
            _eventCenter.OnPlayerArrived += OnPlayerArrived;
            if (_playerMovement) _playerMovement.OnMoveInput += OnPlayerMoveInput;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_eventCenter || !_subscribed) return;
            _eventCenter.OnPlayerArrived -= OnPlayerArrived;
            if (_playerMovement) _playerMovement.OnMoveInput -= OnPlayerMoveInput;
            _subscribed = false;
        }

        private void OnPlayerArrived(object sender, PlayerArrivedEventArgs args)
        {
            // 更新世界坐标并计算格子坐标
            var worldPos = args.TargetWorldPos;
            if (!_gridManager.TryWorldToCellPos(worldPos, out var cell))
            {
                DebugEditor.LogWarning("[PlayerState]:世界坐标转换为格子坐标失败");
                return;
            }

            SetPosition(cell, worldPos, false);
        }

        private void OnPlayerMoveInput(object sender, PlayerInputEventArgs args)
        {
            if (args.MoveDirection == Vector2.zero) return;
            var f = MapDirectionToFacing(args.MoveDirection);
            SetFacing(f, false);
        }

        private Facing MapDirectionToFacing(Vector2 dir)
        {
            // 优先判断明显的X/Y方向
            if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) return dir.x > 0 ? Facing.Right : Facing.Left;

            return dir.y > 0 ? Facing.Up : Facing.Down;
        }

        /// <summary>
        ///     同步设置完整状态并可选择是否通知订阅者
        ///     通知逻辑目前由外部 EventCenter 驱动，因此这里不再重复触发事件中心。
        /// </summary>
        public void SetState(Vector3Int cellPos, Vector2 worldPos, int height, Facing facing, bool notify = true)
        {
            CellPos = cellPos;
            WorldPos = worldPos;
            Height = height;
            Facing = facing;
        }

        public void SetPosition(Vector3Int cellPos, Vector2 worldPos, bool notify = true)
        {
            SetState(cellPos, worldPos, Height, Facing, notify);
        }

        public void SetHeight(int height, bool notify = true)
        {
            SetState(CellPos, WorldPos, height, Facing, notify);
        }

        public void SetFacing(Facing facing, bool notify = true)
        {
            SetState(CellPos, WorldPos, Height, facing, notify);
        }
    }
}