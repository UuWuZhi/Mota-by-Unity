
# PlayerMovement 移动指令缓冲改造计划

## 目标
- 在移动协程进行中只缓存最新一条指令（单步/路径统一处理）。
- 在对话/事件阻断期间不缓存任何指令；进入阻断时清空缓存。
- 如果新指令目标与当前移动协程目标格一致，则丢弃该指令并清空缓存。
- 当前步移动完成时优先执行缓存指令；没有缓存时继续原流程。

## 关键设计
- 新增“最新移动指令缓冲”字段（单一缓存而非队列），同时记录目标格与类型。
- 仅在 `_isMoving == true` 且未阻断时写入缓冲。
- 进入阻断（对话激活或 `_waitingForEventExecution`）时清空缓冲。
- 缓冲指令与当前移动协程目标一致时直接丢弃并清空缓冲。
- “移动完成”回调优先检查缓冲并执行，不再将所有指令排队等待。

## 具体改动点

### 1) PlayerMovement 字段区
- 新增：`_pendingMoveCommand`（仅保存最新指令）、`_pendingTargetCell`、`_currentStepTargetCell`。
- 说明：用于在移动协程进行中缓存最新指令并记录当前步目标格。

### 2) PlayerMovement.ShouldIgnoreMoveInput(...)
- 增加阻断时清空缓冲逻辑。
- 若处于对话或 `_waitingForEventExecution`，直接返回 true 并清空缓冲。

### 3) PlayerMovement.HandleMoveInput(...)
- 若 `_isMoving == true`：
  - 计算目标格并与 `_currentStepTargetCell` 对比。
  - 一致则清空缓冲并丢弃该指令。
  - 不一致则覆盖写入缓冲（只保留最新）。
- 若 `_isMoving == false`：
  - 保持现有逻辑，正常执行单步移动。

### 4) PlayerMovement.TryMoveToCellStep(...)
- 若 `_isMoving == true`：
  - 计算目标格（鼠标路径）并与 `_currentStepTargetCell` 对比。
  - 一致则清空缓冲并丢弃该指令。
  - 不一致则覆盖写入缓冲（只保留最新）。
- 若 `_isMoving == false`：
  - 正常计算路径并 `StartPathMove`。

### 5) PlayerMovement.StartMoveProcess(...) / MoveToTarget(...)
- 在开始单步移动时更新 `_currentStepTargetCell`（由目标世界坐标换算）
- 在移动完成回调中增加：
  - 若存在缓冲指令，立即执行该指令并清空缓冲。
  - 否则按原流程继续（路径移动则 `MoveNextPathStep`，单步输入则结束）。

### 6) PlayerMovement.TryStartPathStep(...)
- 将 `StartMoveProcess(..., () => MoveNextPathStep(token))` 替换为新的完成处理：
  - 如果缓冲存在，切换到新目标并舍弃原路径余量。
  - 否则继续 `MoveNextPathStep(token)`。

## 原则概括：
- 仅在移动协程进行中缓存。
- 阻断期间不缓存，进入阻断即清空。
- 当前步完成后优先执行缓冲指令。
- 新指令与当前目标一致则丢弃并清空缓冲。

