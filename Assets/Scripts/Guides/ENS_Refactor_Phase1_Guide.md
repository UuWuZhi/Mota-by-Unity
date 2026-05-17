# ENS (EventNodeSystem) 重构 Phase 1: 底层数据协议剥离

## 1. 阶段概述与重要声明
本阶段是整个 ENS 架构重构的基石，核心目的是**将现有的 `ScriptableObject` (SO) 降维成无状态的“纯逻辑执行模板”**，并剥离所有的**上下文参数（如：给什么道具，给多少个）**，存放到普通的 `[Serializable]` C# 类中。

### 阶段性标签
- `P1-DataModel`
- `P1-StatelessNodes`
- `P1-ParamExecute`
- `P1-DialogueDeprecated`

⚠️ **【重要声明：对话系统交接】**
**与对话(Dialogue)有关的所有节点均不再维护，也不会参与本次数据解耦重构。** 
此类节点（包括 `ShowDialogue`, `ChoiceDialogueNode`）后续将由 Yarn Integration 模块（如 `YarnFlowNode` 等）在 Yarn 侧通过专用指令流直接取代。在重构期间，保留旧版代码仅供查阅，但不再为其编写新架构的数据类。

> 【实际处理补充】非 Yarn 对话节点（含 `ShowDialogue`、`ChoiceDialogueNode`）已从工程中移除，不再保留旧版代码。

### 补充约定
- **流程控制节点**（包含 Flow 与 Condition 系列）将在 Phase 2 被统一移除，本阶段不为其创建 Data 类，处理方式等同对话节点。
- `SequenceFlowNode` 不做任何处理。
- **无参数节点也需要创建空的 `XXXData` 类型**（用于占位与统一序列化容器）。
- 若目录或命名存在语义混淆，可按实际项目语义调整，不必严格遵循指南路径。

> 【实际处理补充】Flow 系列节点已提前从工程中删除；Condition 节点保留类型但内部逻辑已整体注释，以等待后续统一重构。

---

## 2. 核心增改特性与解决的问题

为了实现“SO享元 + 数据驱动”，本阶段必须实现以下核心特性：

### 特性 1：独立的数据基类 (`BaseNodeData`)
- **要解决的问题**：彻底消除 SO “既是行为又是数据”的强耦合导致的大量重复文件创建。
- **设计逻辑**：创建一个纯粹的、标记了 `[Serializable]` 的数据基类，供未来储存在 `List` 容器中。
- **必要接口**：必须强制包含 `public abstract string GetSummary();` 方法。
  - *目的*：为 Phase 4 中 UI Toolkit 右侧编辑器生成的 ListView 提供清晰的单行摘要字符串（如 `"◆ 获取物品: 苹果 x3"`），避免满屏幕的类名。

### 特性 2：指令模板化与节点无状态化 (Stateless Nodes)
- **要解决的问题**：在旧版模型中，节点通过内部字段（如 `public int itemId`）知晓该做什么；这使得两个相同行为（都是给物品）但参数不同的事件必须创建两个 SO。
- **设计逻辑**：现存的 `ActionNode`、`FlowNode` 及其子类，必须**清空所有序列化数据字段**（除极个别全局配置）。
- **副作用防御**：禁止在 SO 类中定义例如 `float timer`、`bool isFinished` 等运行时状态变量，避免出现多个行为同时使用该单一模板时产生多线程或多实例覆写污染。所有的运行时状态必须交由 `EventRunnerService` 的作用域进行缓存。

### 特性 3：带参注入的执行接口 (Parameterized Execution)
- **要解决的问题**：如果 SO 内没有了参数字段，它该如何执行逻辑？
- **设计逻辑**：重构并替换原本的基类 `Execute` 签名。
  - *旧*：`public abstract void Execute(EventNodeContext ctx, Action onComplete);`
  - *新*：`public abstract void Execute(BaseNodeData data, EventNodeContext ctx, Action onComplete);`
  - *流程*：`EventRunnerService` 在读取到包含参数的 `BaseNodeData` 后，调用该节点享元 SO，并将这段数据与 `EventNodeContext` 一并传入以保持原有依赖与异步完成机制。

> 【实际处理补充】现阶段仅改造节点侧签名与数据读取，`EventRunnerService` 的适配留待后续阶段统一处理。

---

## 3. 具体涉及的文件修改清单

*所有路径前缀基于 `Assets/Scripts/Modules/EventNodeSystem/Runtime/`*

### 📝 1. 新增/创建的文件
| 文件路径 | 修改类型 | 说明与职责 |
| :--- | :--- | :--- |
| `Core/BaseNodeData.cs` | **新增** | 定义 `[Serializable] public abstract class BaseNodeData` 以及 `GetSummary()` 方法。 |
| `Nodes/Action/Data/XXXData.cs` | **新增** | （举例）针对非对话类的 Action 节点，创建其具体参数类。例如若有 `ModifyItemNode`，则需创建 `ModifyItemData : BaseNodeData { public int itemId; public int count; }`。 |
| `Nodes/Flow/Data/XXXData.cs` | **新增** | （举例）针对非对话类的 Flow 节点，创建如 `SequenceFlowData : BaseNodeData { ... }` 之类的参数实体。 |

### 🛠️ 2. 修改与重构的文件
| 文件路径 | 修改类型 | 说明与职责 |
| :--- | :--- | :--- |
| `Core/ENSNode.cs` (或 `ActionNode.cs` / `FlowNode.cs` 等基类) | **修改** | 1. 清除内部上下文序列化字段。<br>2. 修改 `Execute` 方法签名为 `Execute(BaseNodeData data, EventRunnerService runner)`。 |
| `Runner/EventRunnerService.cs` | **修改** | 修改原有的调用栈，让它在驱动当前操作时，能够获取并传递相应的 `BaseNodeData` 参数至 Node 的新 `Execute` 接口。 |
| `Nodes/Flow/SequenceFlowNode.cs` | **不处理** | 该节点将在 Phase 2 直接删除，本阶段不做改动以避免重复迁移成本。 |
| `Nodes/Action/XXXNode.cs` (保留维护的非对话节点) | **修改** | （例如物品修改、动画播放、声音播放等节点），移除原有私有字段，代码内部改为读取 `data as ModifyItemData` 取值。 |

### 🚫 3. 标记废弃/停止维护的文件 (Deprecated)
*以下文件在本阶段**不要进行任何数据类提取和接口修改**。保留它们仅为了兼容未迁移完毕的老事件，并加上 `[Obsolete]` 特性。*

| 文件路径 | 说明与职责 |
| :--- | :--- |
| `Nodes/Action/ShowDialogue.cs` | 已被 Yarn 系统接管。不再生成对应的 `ShowDialogueData`，不再梳理其内部逻辑。 |
| `Nodes/Flow/ChoiceDialogueNode.cs` | 选项分支逻辑已全面移交至 `YarnRouteBridge` / `YarnFlowNode`，同样做废弃处理弃置修改。 |

> 【实际处理补充】上述节点已直接删除，未保留 `[Obsolete]` 标记版本。

---

## 4. 进度追踪与开发时间线 (Timeline)

| 状态 | 预期时间 | 检查点 (Checkpoint) / 任务描述 | 达成判定标准 |
| :---: | :---: | :--- | :--- |
| 🔄 | **Step 1** | **数据层基建敲定**<br>定义 `BaseNodeData` 与基础节点接口签名修改。 | 工程报错数激增，所有原有老节点的 `Execute` 签名报 Override 失败。 |
| 🔄 | **Step 2** | **具体数据实体补全 & 老节点重构**<br>根据目前系统中依然维护的 Action 逐个创建 Data 类，并适配新签名。 | Node 相关的编译错误全部消除。每个 Node 能够成功向下转型接收到自己的 Data。 |
| 🔄 | **Step 3** | **引擎枢纽梳理 (EventRunnerService)**<br>修改 Runner 逻辑以支持传递 `data` 参数。为 Phase 2 引入序列化数组 (`IP` 指针设计) 做好热身基础。 | 无编译错误，Runner 能手工传入临时构建的 Data 测试跑通。 |
| 🔄 | **Step 4** | **代码清理与废弃标记**<br>将上述的对话类节点标记 `[Obsolete("Use Yarn nodes instead.")]`。 | Inspector 不再提示由于这些类引发的歧义分析。 |

**当前进度**：
- Step 1 已完成（`BaseNodeData` 创建、`Execute` 签名已改为 `BaseNodeData + EventNodeContext`）。
- Step 2 已完成（非对话 Action 数据类与节点改造已完成，包含无参数空 Data）。
- Step 3 已完成最小改造（`EventRunnerService` 支持传入 `BaseNodeData`，但未引入 `EventSequence/ENSRegistry`）。
- Step 4 已以“直接删除旧节点”的方式完成（未保留 `[Obsolete]` 版本）。

**当前收尾说明**：
- 已完成调用方适配：`EventTileManager` 与 `ItemUseHandler` 传入 `null` 作为 `dataList` 占位以保持编译通过。

**与 Phase 2 的衔接点**：
- `EventRunnerService` 需要在 Phase 2 接入 `EventSequence` + `ENSRegistry`，替换当前 `EventNode`/`List<EventNode>` 执行入口。
- 现有 `Run/RunAndWait/RunActions` 仍是 Phase 1 兼容入口，Phase 2 需逐步替换为 `StartSequence`。
- `RunActions` 的 `dataList` 仅做顺序对齐，Phase 2 需改为基于指令指针与 `Label/Jump` 的执行模型。

- *注：本阶段结束后，ENS 在场景中将暂时处于“瘫痪态”（因为旧的连线被断开了，而新的事件页容器 (EventSequence) 将在 Phase 2 实现）。*
- *注2：在 Phase 1 开发落地时，如果遇到比如“有些节点本身在特定时机需要进行 await 或者 yield return 操作”的情况，记得确保其 runner（EventRunnerService）中依然能够持有用于异步调用的 Coroutine 引用，因为 SO 已经变成单一实例模板了。*