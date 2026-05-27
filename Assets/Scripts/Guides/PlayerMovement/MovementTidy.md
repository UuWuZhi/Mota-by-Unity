
# PlayerMovement 清理建议计划

## 目标
- 明确输入、缓冲、执行、路径控制四类职责，减少相互耦合。
- 统一单步移动入口，避免执行逻辑重复与遗漏。
- 缓冲逻辑集中在“步完成回调”消费，其他位置只写入/清空。
- 阻断判断集中处理，避免散落。

## 规划原则
1. **入口唯一**：单步移动只保留一个执行入口（例如 `ExecuteStepMove`）。
2. **职责单一**：输入处理不直接涉及移动细节，路径控制不关心缓冲细节。
3. **状态收敛**：合并/减少状态字段，避免重复保存同一语义。
4. **统一回调**：移动完成后统一走“缓冲判断→执行/继续路径”。

## 结构调整建议

### 1) 输入与缓冲层
- **职责**：只负责判断是否接收输入、是否写入缓冲、是否覆盖最新指令。
- **建议方法**：
  - `ShouldIgnoreMoveInput(...)`：只做阻断判断（对话/事件执行）。
  - `CacheStepMoveCommand(...)` / `CachePathMoveCommand(...)`：只写入缓冲，完全不触发移动。

### 2) 单步执行层
- **职责**：只负责“执行一步移动”，包含方向更新、通行检查、事件判断、启动协程。
- **建议方法**：
  - `ExecuteStepMove(...)` 作为唯一入口。
  - `UpdateMoveDirection(...)` 只在此入口调用。
  - `ClearPathLine()` 在开始单步前处理，避免残留。

### 3) 路径控制层
- **职责**：只负责路径生成与步进，移动执行依赖单步入口。
- **建议方法**：
  - `ExecutePathMove(...)` 统一路径入口（替代 `TryMoveToCellStep` 中的重复逻辑）。
  - `MoveNextPathStep(...)` 内仅负责取下一步与计算方向，移动执行交给 `ExecuteStepMove(...)` 或 `TryStartPathStep(...)`。

### 4) 缓冲消费层
- **职责**：只在“步完成时”判断缓冲并执行。
- **建议方法**：
  - `HandlePendingMoveAfterStep(...)` 作为唯一消费入口。
  - 任何移动入口都不直接消费缓冲，避免重复与竞态。

## 运行时状态量梳理与更新位置

### 移动状态
- `_isMoving`：表示当前是否处于移动协程执行中。
  - **更新位置**：`MoveToTarget` 开始时设为 true，协程结束时设为 false。
  - **不可替代原因**：仅此字段能准确表达“协程进行中”状态。

- `_waitingForEventExecution`：表示是否被事件执行阻断。
  - **更新位置**：`StartMoveProcess` 中根据 `blockUntilComplete` 设置；事件完成回调中清为 false。
  - **不可替代原因**：阻断状态与移动状态分离，不能用 `_isMoving` 表示。

- `_moveToken`：用于中断旧移动协程。
  - **更新位置**：`StartMoveProcess` 前递增；取消移动时递增。
  - **不可替代原因**：用于协程一致性校验，替代不了。

- `_moveCoroutine`：保存当前协程引用。
  - **更新位置**：`StartMoveProcess` 创建时赋值；`StopMoveCoroutine` 清空。
  - **不可替代原因**：用于协程停止与状态清理。

- `_currentStepTargetCell`：记录当前单步移动的目标格。
  - **更新位置**：`StartMoveProcess` 计算 `targetPos` 后赋值；`MoveToTarget` 结束时清空。
  - **不可替代原因**：用于判断“缓冲指令是否与当前步重复”。
  - **注意**：不可与 `_pendingTargetCell` 合并（语义不同）。

### 路径移动状态
- `_isPathMoving`：表示是否处于路径移动状态。
  - **更新位置**：`StartPathMove` 设为 true；`FinishPathMove` 设为 false。
  - **不可替代原因**：路径状态与单步状态分离。

- `_pathMoveToken`：用于中断旧路径流程。
  - **更新位置**：`StartPathMove` 递增；`CancelPathMove` 递增。
  - **不可替代原因**：路径流程一致性校验。

- `_pathQueue`：保存路径队列。
  - **更新位置**：`StartPathMove` 初始化；`FinishPathMove` 清空。
  - **不可替代原因**：路径步进依赖真实队列。

- `_pathTargetCell`：记录路径终点格。
  - **更新位置**：`StartPathMove` 设置；`FinishPathMove` 清空。
  - **不可替代原因**：用于判断终点步与路径终止。

### 缓冲指令状态
- `_pendingMoveCommandType`：记录缓冲指令类型（无/单步/路径）。
  - **更新位置**：`CacheStepMoveCommand` / `CachePathMoveCommand` 设置；`ClearPendingMoveCommand` 清空。
  - **不可替代原因**：区分缓冲类型是必需的。

- `_pendingMoveDirection`：缓冲单步方向。
  - **更新位置**：`CacheStepMoveCommand` 设置；`ClearPendingMoveCommand` 清空。
  - **不可替代原因**：与 `_pendingTargetCell` 语义不同，不应合并。

- `_pendingTargetCell`：缓冲路径目标。
  - **更新位置**：`CachePathMoveCommand` 设置；`ClearPendingMoveCommand` 清空。
  - **不可替代原因**：路径目标格与单步方向无法互换。

## 合并/收敛建议
- `_moveDir` 若不再作为状态输出或动画驱动，建议移除或仅保留局部变量。
- `_targetWorldPos` 若只在一步移动内部使用，可考虑改为局部变量；若事件与检查依赖则保留。

## 具体落地步骤（建议顺序）
1. 统一单步执行入口（抽出 ExecuteStepMove）。
2. 移除 `HandleMoveInput` 与 `ExecuteBufferedStepMove` 中的重复逻辑。
3. 路径执行统一入口（抽出 ExecutePathMove）。
4. 缓冲只保留“写入/清空”与“步完成消费”两处。
5. 收敛状态字段（如 `_currentStepTargetCell` 与 `_pendingTargetCell` 的职责边界）。

## 需要重点关注的细节
- `UpdateMoveDirection` 应只在单步执行入口调用。
- `ClearPathLine` 只在单步执行入口或路径终止时调用。
- 阻断判断集中处理，避免某些入口漏掉阻断逻辑。

