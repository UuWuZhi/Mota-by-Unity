# ENS (EventNodeSystem) 重构 Phase 2: 执行流与分支逻辑重构

## 1. 阶段概述与重要声明
经历了 Phase 1 的数据剥离后，现在的节点已经被“拍扁”成了纯粹的配置数据。本阶段的核心目的是**彻底摧毁旧有的“图论连线/节点引用”式的事件流转机制，建立基于“一维数组+指令指针(IP)”的新型执行引擎。**

⚠️ **【重要声明：对话系统交接】**
- **与对话(Dialogue)及对话选项(Choice)有关的节点均不再维护，也不会参与本次流控制重构。** 

⚠️ **【边界澄清：Yarn 与 ENS 的职责交接】**
- **Yarn 的职责仅限“视图层 (View)”**：Yarn 节点只负责做“屏幕显示”与对话流程演出。
- **ENS 的职责为“控制层 (Controller)”**：**真实的逻辑判断（如扣除金币、判断金币是否足够、发放奖励等属性检测）必须且只能完全由 ENS 控制**。当 ENS 判定条件不满足时，通过跳转逻辑绕过特定的 Yarn 行为节点展现。

---

## 2. 核心增改特性与解决的问题
- 采用 **预编译扁平表驱动 (Flat-Table Execution)** 技术。
- 采用**汇编式跳转 (Assembly-Style Goto)** 作为唯一的流控制方案，抛弃一切嵌套列表结构。
- **必须提供 `Label` 与 `Jump` 节点** 作为底层统一跳转机制，本阶段以稳定 `Goto` 核心为目标，结构化的 `If/For` 封装放在本阶段收尾追加。

### 特性 1：事件序列容器 (`EventSequence`)
- **要解决的问题**：独立的数据类(`BaseNodeData`)需要一个统一的载体，以便挂载在场景中的 NPC 或触发器上。并且需彻底抹除诸如旧版 `SequenceFlowNode.GetRequiredServices()` 那样层层递归获取依赖的冗余操作。
- **设计逻辑**：创建一个纯数据类容器，内部包裹 `[SerializeReference] public List<BaseNodeData> Commands`。它相当于大杂烩，是未来所有事件执行的唯一入口资源。

### 特性 2：享元工厂与指令派发器 (Flyweight Dispatcher)
- **要解决的问题**：在旧版中，`Runner.Execute(Node)` 是因为拿到的是具体的 Node 对象。现在 `EventSequence` 里存的只有 `XXXData`，系统需要知道遇到 `ModifyItemData` 时，该调用哪个逻辑模板来执行。
- **设计逻辑**：建立 `ENSRegistry`，缓存 `<Type, ENSNode>` 映射字典。根据数据类型分发给无状态实例。

### 特性 3：带指令指针驱动的执行引擎 (Instruction Pointer Execution)
- **要解决的问题**：取代旧版节点内自己调用 `runner.SetNextNode(nextNode)` 的链式调用。使得无论节点内部怎么写，程序总是按照列表自上而下运行。
- **设计逻辑**：`EventRunnerService` 中新增 `int currentIndex`。
  - 执行开始前，重置 `currentIndex = 0`。
  - 读取 `Commands[currentIndex]` 并利用特性 2 派发执行。
  - 当前节点通过 `await` 或事件回调告知执行完毕后，引擎自动 `currentIndex++`，继续下一跳。

### 特性 4：汇编控制：标签 (`Label`) 与 跳转 (`Jump`)
- **要解决的问题**：实现非线性的逻辑判断（如条件分支、循环），而不增加列表的层级深度。
- **设计逻辑**：
  - **标签机制**：引入 `LabelData`（仅含有一个 `string labelName` 属性）。执行引擎遇到它时**什么也不做**，直接跳过。但在执行刚开始时，引擎会预扫描整个列表，生成 `Dictionary<string, int> labelToCommandIndex` 缓存。
  - **跳转机制**：引入 `JumpData`（或条件跳转 `BranchData`，包含 `string targetLabel` 和判定条件）。引擎遇到并判定条件成立后，从缓存字典查出目标 Index，直接修改 `EventRunnerService.currentIndex = targetIndex - 1`，从而实现 GOTO。

### 特性 5：结构化语法糖的收尾追加 (If/For)
- **要解决的问题**：项目需要结构化编排体验，但直接引入会阻塞主线跳转稳定性。
- **设计逻辑**：先完成 `Label` 与 `Jump` 的稳定底座，再在本阶段收尾追加 `If/For` 的语法糖节点，通过“预编译到 Jump”的方式实现并保留统一跳转内核。

---

## 3. 具体涉及的文件修改清单

*所有路径前缀基于 `Assets/Scripts/Modules/EventNodeSystem/Runtime/`*

### 📝 1. 新增/创建的文件
| 文件路径 | 修改类型 | 说明与职责 |
| :--- | :--- | :--- |
| `Core/EventSequence.cs` | **新增** | 定义 `[Serializable] public class EventSequence`，包含核心组件 `[SerializeReference] public List<BaseNodeData> Commands`。 |
| `Core/ENSRegistry.cs` | **新增** | （或 `ENSNodeFactory`），在依赖注入 (VContainer) 阶段，扫描或手动注册 `<Type, ENSNode>`，实现基于类型的无状态节点派发机制。 |
| `Nodes/Flow/Data/LabelData.cs` | **新增** | 继承 `BaseNodeData`。纯标记类，包含 `public string labelName;`。`GetSummary()` 返回 `$"▶ [标签] {labelName}"`。 |
| `Nodes/Flow/Data/JumpData.cs` | **新增** | （以及配套的条件 Jump），包含 `public string targetLabelName;`。 |
| `Nodes/Flow/LabelNode.cs` | **新增** | 对接 `LabelData` 的执行器。核心逻辑极简：立刻向系统返回执行完毕（无表现）。 |
| `Nodes/Flow/JumpNode.cs` | **新增** | 对接 `JumpData` 的执行器。调用 `EventRunnerService.JumpToLabel(data.targetLabelName)`。 |

### 🛠️ 2. 修改与重构的文件
| 文件路径 | 修改类型 | 说明与职责 |
| :--- | :--- | :--- |
| `Runner/EventRunnerService.cs` | **重构** | 1. 废弃原先的 `StartEvent(ENSNode node)` 接口。<br>2. 新增 `StartSequence(EventSequence sequence)` 接口。<br>3. 引擎内部维护 `int _currentIndex` 和 `Dictionary<string, int> _labelMap`。<br>4. 增加预编译预扫描流程 (扫出所有的 Label及其下边)。<br>5. 增加 `public void JumpToLabel(string labelName)` 开放接口。 |

### 🚫 3. 彻底删除/废弃的文件 (Deletion)
*以下这些属于旧版流控制体系的核心，由于架构已完全变更，直接移除。*

| 文件路径 | 修改类型 | 说明与职责 |
| :--- | :--- | :--- |
| `Nodes/Flow/SequenceFlowNode.cs` | **删除** | 一维数组本身就是 Sequence，该节点逻辑失去存在的意义。彻底废弃。 |
| `Nodes/Flow/ChoiceDialogueNode.cs` | **删除** | 明确由 Yarn侧节点处理 |
| `Nodes/Flow/IfFlowNode.cs` | **删除** | 将老版负责处理 `trueNode`, `falseNode` 的逻辑直接连根拔除，抛弃树状分支思维。 |
| `Nodes/Flow/SwitchFlowNode.cs` | **删除** | 原因同上 |

---

## 4. 进度追踪与开发时间线 (Timeline)

| 状态 | 预期时间 | 检查点 (Checkpoint) / 任务描述 | 达成判定标准 |
| :---: | :---: | :--- | :--- |
| 🔄 | **Step 1** | **容器与注册表搭建**<br>创建 `EventSequence` 以及 `ENSRegistry` 工厂。 | 能够在此处通过 `data.GetType()` 正确获得单一对应的、无状态的 `ENSNode` 享元实例。 |
| 🔄 | **Step 2** | **主导引警 IP 循环重构**<br>重写 `EventRunnerService`，植入 `currentIndex` 和 `StartSequence()` 的 `List` 循环读取流程。预测试自动 `currentIndex++` 逻辑。 | `EventRunnerService` 可以在空白场景中接收一个自建的含有多个 Action Data 的 Sequence 并全自动按序跑完。 |
| 🔄 | **Step 3** | **汇编指令实装 (Label & Jump)**<br>添加 `LabelData/Node` 与 `JumpData/Node`，编写预扫描构建 `_labelMap` 的逻辑。 | 在测试 Sequence 中设置 `Jump`，引擎能够正确根据 Label 名称跳过特定 Action 执行。 |
| 🔄 | **Step 4** | **无限循环(Deadlock)安全防御**<br>为引擎的 Jump 机制加入安全锁。 | 当检测到无延时或无异步操作的死循环式 `Jump` 时（防卡死），直接中断 `EventRunnerService` 的循环并抛出严重警告报错。 |

*注：本阶段将奠定新版 ENS 的全部运行时基础。完成本阶段后，游戏内的逻辑层已经可以完全跑通。`If/For` 语法糖在本阶段收尾通过“预编译到 Jump”的方式追加。*
