# ENS (EventNodeSystem) 重构 Phase 2: 执行流与分支逻辑重构

## 1. 阶段概述与重要声明
经历了 Phase 1 的数据剥离后，现在的节点已经被“拍扁”成了纯粹的配置数据。本阶段的核心目的是**彻底摧毁旧有的“图论连线/节点引用”式的事件流转机制，建立基于“一维数组+指令指针(IP)”的新型执行引擎。**

### 阶段性标签
- `P2-SequenceContainer`
- `P2-GotoCore`
- `P2-LabelJump`
- `P2-IfForWrap`

⚠️ **【重要声明：对话系统交接】**
- **与对话(Dialogue)及对话选项(Choice)有关的节点均不再维护，也不会参与本次流控制重构。** 

⚠️ **【边界澄清：Yarn 与 ENS 的职责交接】**
- **Yarn 的职责仅限“视图层 (View)”**：Yarn 节点只负责做“屏幕显示”与对话流程演出。
- **ENS 的职责为“控制层 (Controller)”**：**真实的逻辑判断（如扣除金币、判断金币是否足够、发放奖励等属性检测）必须且只能完全由 ENS 控制**。当 ENS 判定条件不满足时，通过跳转逻辑绕过特定的 Yarn 行为节点展现。

### ✅ Phase 2 任务清单（含 Phase 1 衔接点）
> 依据三份 Guide 归纳，以下为 Phase 2 必须完成的明确任务。

1. **接入 `EventSequence` + `ENSRegistry`（替换旧入口）**
   - 目标：取代 `EventNode` / `List<EventNode>` 执行入口。
   - 衔接点：Phase 1 已完成 `BaseNodeData` 与节点无状态化，现在必须让 Runner 面向 `EventSequence` 驱动。
2. **替换 `Run/RunAndWait/RunActions` 为 `StartSequence` 主入口**
   - 目标：逐步移除 Phase 1 兼容入口，统一切换到 `StartSequence`。
   - 衔接点：`RunActions` 的 `dataList` 只是顺序对齐占位，Phase 2 必须改为 IP 指针模型。
3. **实现指令指针执行引擎（IP）**
   - 目标：引入 `currentIndex` 顺序读取 `Commands`，执行完后自动 `currentIndex++`。
4. **实现 `Label` / `Jump` 汇编级跳转**
   - 目标：预扫描建立 `labelToIndex`，Jump 修改 `currentIndex` 进行跳转。
5. **建立死循环防护机制（Jump Deadlock Guard）**
   - 目标：检测无延时的无限跳转并中断执行，输出严重警告。
6. **清理旧 Flow 控制节点残留**
   - 目标：确保 `SequenceFlowNode`、`IfFlowNode`、`SwitchFlowNode` 等不再参与编译与运行。
7. **确保 `EventRunnerService` 仍可托管协程能力**
   - 目标：保留 `RunCoroutine` 作为异步节点的运行支点（Phase 1 收尾提醒）。
8. **补充多序列并发与中断机制（新增）**
   - 目标：当旧序列因异步节点挂起时，允许新触发序列排队并在可执行窗口切换运行，避免状态覆盖。

---

## 2. 核心增改特性与解决的问题
- 采用 **预编译扁平表驱动 (Flat-Table Execution)** 技术。
- 采用**汇编式跳转 (Assembly-Style Goto)** 作为唯一的流控制方案，抛弃一切嵌套列表结构。
- **必须提供 `Label` 与 `Jump` 节点** 作为底层统一跳转机制，本阶段以稳定 `Goto` 核心为目标，结构化的 `If/For` 封装放在本阶段收尾追加。

### 特性 1：事件序列容器 (`EventSequence`)
- **要解决的问题**：独立的数据类(`BaseNodeData`)需要一个统一的载体，以便挂载在场景中的 NPC 或触发器上。并且需彻底抹除诸如旧版 `SequenceFlowNode.GetRequiredServices()` 那样层层递归获取依赖的冗余操作。
- **设计逻辑**：创建一个纯数据类容器，内部包裹 `[SerializeReference] public List<BaseNodeData> Commands`。它相当于大杂烩，是未来所有事件执行的唯一入口资源。

#### ✅ 当前完成情况
- 已新增 `Core/EventSequence.cs`，包含 `[SerializeReference] public List<BaseNodeData> Commands`。
- 已作为 Phase 2 容器基础落地，可用于挂载在触发器/NPC 上承载指令列表。

### 特性 2：享元工厂与指令派发器 (Flyweight Dispatcher)
- **要解决的问题**：在旧版中，`Runner.Execute(Node)` 是因为拿到的是具体的 Node 对象。现在 `EventSequence` 里存的只有 `XXXData`，系统需要知道遇到 `ModifyItemData` 时，该调用哪个逻辑模板来执行。
- **设计逻辑**：建立 `ENSRegistry`，缓存 `<Type, ENSNode>` 映射字典。根据数据类型分发给无状态实例。

#### ✅ 当前完成情况
- 已新增 `Core/ENSRegistry.cs`，支持按 `BaseNodeData` 类型注册与查询节点模板。
- 已新增 `Core/NodeMappingTableSO.cs`，用于保存 Data-Node 映射表资产。
- 已新增编辑器生成器 `Editor/EventNodeSystem/ENSRegistryGenerator.cs`，可批量扫描并生成 `Assets/Resources/ENS/NodeMappingTable.asset`。
- 已移除 `ENSRegistry.generated.cs` 的 VContainer 注册方案，改为 Resources 资产加载。
 - `EventRunnerService` 构造后会立即加载 `NodeMappingTable` 并填充 `ENSRegistry`，运行时派发入口已建立。

#### ENSRegistry 功能细节
- **要解决的问题**：`EventSequence` 只存 `BaseNodeData`，无法直接定位到对应的节点模板（SO）。需要统一的类型映射与派发入口。
- **解析方式**：
  - 以 `BaseNodeData` 的运行时类型作为键（如 `ModifyItemData`）。
  - `ENSRegistry` 提供 `GetNode(Type dataType)` 或 `TryGetNode<TData>(out ENSNode node)`，返回对应的无状态节点 SO。
  - 映射建立方式可用手动注册或反射扫描，但必须最终落在稳定的 `Dictionary<Type, ENSNode>` 上。
- **职责边界**：
  - `ENSRegistry` **只负责映射与提供节点模板**，不执行节点逻辑。
  - `EventRunnerService` **负责驱动与传参**：读取 `BaseNodeData`，通过 `ENSRegistry` 获取节点模板，调用 `node.Execute(data, ctx, onComplete)`。
- **与 Runner 的配合方式**：
  1. `EventRunnerService` 取出 `Commands[currentIndex]` 得到 `data`。
  2. 调用 `ENSRegistry.GetNode(data.GetType())` 获取节点模板。
  3. 由 Runner 调用 `node.Execute(data, ctx, onComplete)`，并推进 `currentIndex`。
- **是否合并到 Runner**：
  - 可以将映射逻辑内置到 Runner，但会导致 Runner 既负责执行又负责注册映射，职责过重且不利于后续测试与替换。
  - 保持独立 `ENSRegistry` 更清晰，Runner 仅依赖其读取映射即可。

#### 代码生成注册方案（历史方案，已弃用）
- **要解决的问题**：节点与数据的映射是稳定关系，但数量大、手动维护易出错，需要自动化生成可维护的注册表。
- **历史生成方式**：
  - 通过构建脚本或工具扫描 `EventNode` 子类与对应的 `BaseNodeData` 类型，生成 `ENSRegistry.generated.cs`。
  - 生成文件包含一个静态注册方法，例如 `RegisterAll(ENSRegistry registry, IObjectResolver resolver)`，在启动阶段一次性写入映射。
- **历史生成内容要求**：
  - 每个节点条目生成一行显式注册，例如 `registry.Register<ModifyItemData>(resolver.Resolve<ModifyItemNode>());`。
  - 显式列出所有映射，避免运行时反射与 AOT 裁剪问题。
- **历史触发时机**：
  - 在启动阶段或容器构建完成后调用 `RegisterAll(...)`。
  - 不在运行时动态扫描，确保运行时开销为零且行为可预测。
- **历史与 Runner 的配合**：
  - `EventRunnerService` 仅依赖 `ENSRegistry` 的映射结果，不参与注册逻辑。
  - 映射更新完全由生成流程控制，新增/删除节点时同步更新生成文件。

> 【方案调整（最终定稿）】当前已转为 **Resources 映射表资产方案**，上方代码生成方案仅作为历史记录保留，不再作为当前实现目标。

#### Resources 映射表资产方案（当前实施方案）
- **要解决的问题**：避免 VContainer 依赖与命名规则脆弱性，同时提供可视化映射资产与运行时稳定加载。
- **生成方式**：
  - 使用编辑器工具扫描 `EventNode` 子类与对应的 `BaseNodeData` 类型，生成 `NodeMappingTableSO` 资产。
  - 资产放在 `Assets/Resources/ENS/NodeMappingTable.asset`，运行时直接加载。
- **运行时加载方式**：
  - 启动阶段调用 `Resources.Load<NodeMappingTableSO>("ENS/NodeMappingTable")`。
  - 解析为 `Dictionary<Type, EventNode>` 后写入 `ENSRegistry`。

##### 【当前实现状况与需要修改的地方】
1. **已存在：** `ENSRegistryGenerator.cs` 与 `ENSRegistry.generated.cs`（使用 `resolver.Resolve<Node>()` 注册）。
   - **处理结果：** 已将运行时依赖切换到 `NodeMappingTableSO` 资产，`ENSRegistry.generated.cs` 仅保留为历史记录，不再作为当前流程。
2. **已存在：** `ENSRegistry.generated.cs` 使用 VContainer 解析节点。
   - **处理结果：** 运行时已改为加载 `NodeMappingTableSO` 并填充 `ENSRegistry`。
3. **缺失：** `NodeMappingTableSO` 资产类型与运行时加载逻辑。
   - **处理结果：** 已补齐资产定义类、生成器输出逻辑、运行时加载入口。
4. **Runner 配合点：** `EventRunnerService` 目前依赖 `ENSRegistry`，但未明确注册来源。
   - **处理结果：** Runner 初始化时已自动调用映射资产加载并填充 `ENSRegistry`，`StartSequence` 可正确解析。

### 特性 3：带指令指针驱动的执行引擎 (Instruction Pointer Execution)
- **要解决的问题**：取代旧版节点内自己调用 `runner.SetNextNode(nextNode)` 的链式调用。使得无论节点内部怎么写，程序总是按照列表自上而下运行。
- **设计逻辑**：`EventRunnerService` 中新增 `int currentIndex`。
  - 执行开始前，重置 `currentIndex = 0`。
  - 读取 `Commands[currentIndex]` 并利用特性 2 派发执行。
  - 当前节点通过 `await` 或事件回调告知执行完毕后，引擎自动 `currentIndex++`，继续下一跳。

#### ✅ 进入条件评估
- 特性 1、2 已满足进入条件：`EventSequence` 已落地，`ENSRegistry` 可在 Runner 初始化后加载映射表并派发。
- 可以开始特性 3 的实现。

#### ✅ 当前完成情况
- 已完成 `EventRunnerService` 的指令指针执行改造：使用 `_currentIndex` 顺序执行 `EventSequence.Commands`。
- 已新增 `StartSequence` 与 `RunSequenceAndWait` 入口，替代旧的 `RunActions` 接口。
- `EventTileManager` 与 `ItemUseHandler` 已切换为事件序列入口。
- `EventNodeTile` 与 `ItemData` 已改为持有 `EventSequence`。
- 旧的 `Run/RunActions/RunAndWait` 入口已移除（接口层面）。

#### ✅ 是否可进入特性 4（Label/Jump）
- 当前执行引擎已具备线性指令驱动能力，**可以进入特性 4**。
- 进入前提均满足：序列容器、注册表派发与指针执行已落地。

#### EventRunnerService 改造细则
- **要解决的问题**：当前 `EventRunnerService` 面向 `EventNode` 与 `EventNodeContext`，支持单节点与列表顺序执行，无法驱动 `EventSequence + BaseNodeData` 的线性指令流。
- **操作对象变化**：从 `EventNode`/`List<EventNode>` 转为 `EventSequence`/`List<BaseNodeData>`，并通过 `ENSRegistry` 派发到无状态节点模板。
- **函数保留/移除/修改**：
  - **保留**：`RunCoroutine`（继续作为协程宿主能力）。
  - **移除**：`Run(EventNode rootNode, ...)`、`RunAndWait(...)`、`RunActions(...)`、`RunActionsAndWait(...)` 与 `RunActionsSequence(...)`（这些均以节点引用为中心，无法对接 `EventSequence`）。
  - **新增**：`StartSequence(EventSequence sequence, EventNodeContext ctx, Action onComplete)` 作为唯一入口，内部使用 `currentIndex` 驱动执行。
  - **新增**：`JumpToLabel(string label)` 或 `JumpToIndex(int index)` 作为跳转入口。
  - **调整**：`RegisterRequiredServices(...)` 的调用位置从“节点集合扫描”改为“Runner 启动时按需注册”，并允许被执行节点在首次触发时按需补齐服务。
- **核心流程变化**：
  1. 进入 `StartSequence` 后构建 `labelToIndex` 映射并重置 `currentIndex`。
  2. 取出 `Commands[currentIndex]`，通过 `ENSRegistry` 找到对应的无状态节点模板。
  3. 调用 `node.Execute(data, ctx, onComplete)`，完成后 `currentIndex++`。
  4. `Jump` 或 `If/For` 触发时修改 `currentIndex`，继续线性执行。
- **依赖增减**：
  - **新增依赖**：`ENSRegistry`（或等价工厂）作为数据类型到模板的映射入口。
  - **保留依赖**：`CoroutineRunner` 以及 `GridManager`、`IInventoryService`、`PlayerAttribute`、`EventCenter`、`MapManager` 等仍由 `EventNodeContext` 提供。
  - **减少依赖**：`EventNode` 本体与节点列表执行入口不再需要直接传入 Runner。

### 特性 4：汇编控制：标签 (`Label`) 与 跳转 (`Jump`)
- **要解决的问题**：实现非线性的逻辑判断（如条件分支、循环），而不增加列表的层级深度。
- **设计逻辑**：
  - **标签机制**：引入 `LabelData`（仅含有一个 `string labelName` 属性）。执行引擎遇到它时**什么也不做**，直接跳过。但在执行刚开始时，引擎会预扫描整个列表，生成 `Dictionary<string, int> labelToCommandIndex` 缓存。
  - **跳转机制**：`JumpData` 继续作为**唯一底层跳转原语**。它负责无条件跳转到目标标签；如果需要条件控制，则不再新增一套独立的 `Goto` 基础节点，而是把“条件判断 + 跳转目标”统一收敛到一个**条件跳转语义层**（可命名为 `BranchJump` / `GuardedJump`，最终名称以后续实现为准）。
  - **条件表达**：条件能力不放在 `Jump` 之外单独造一套嵌套控制树，而是以**平铺式条件描述**的方式挂在跳转语义层上，支持单条件、取反、多个条件的 `All/Any` 组合，避免引入难维护的节点嵌套。
  - **双轨执行策略**：编辑时使用 `Label` 提升可读性，运行时预编译成索引跳转。`JumpData` 保留标签名，Runner 在预扫描阶段写入目标索引缓存，执行时直接走索引。

#### ✅ 当前完成情况
- 已新增 `LabelData` / `JumpData` 数据类与 `LabelNode` / `JumpNode` 执行器。
- Runner 在 `StartSequence` 时预扫描 `LabelData` 并构建 `_labelMap`（标签名 -> 索引）。
- `JumpNode` 通过 `EventRunnerService.JumpToLabel` 进行跳转，执行时直接走索引。
- 已补充死循环防护代码：包含步数上限与同帧索引回访上限，异常时自动中断并输出 `[ENS-DeadlockGuard]` 日志。
- 已完成并发调度新定案落地：任务级状态（Ready/Running/Blocked/Completed/Aborted）、`_blockedRuns` 阻塞容器、`_readyQueue` 就绪队列、阻塞回调回归就绪队列继续执行。

### 特性 5：结构化语法糖的收尾追加 (If/For)
- **要解决的问题**：项目需要结构化编排体验，但直接引入会阻塞主线跳转稳定性。
- **设计逻辑**：先完成 `Label` 与 `Jump` 的稳定底座，再在本阶段收尾追加 `If/For` 的语法糖节点，通过“预编译到 Jump”的方式实现并保留统一跳转内核。
- **当前编辑器限制**：`Tri-Inspector` 对 `[SerializeReference]` 的多态展开仅支持第一层。像 `If` 节点内部继续嵌套 `Condition` 的编辑能力，已经超出 Phase 2 的原生 Inspector 能力边界，必须交由 Phase 3 的自定义编辑器实现。

#### ✅ 执行方案细化（特性5推进前）
1. **定义语法糖数据类**
   - 新增 `IfData` / `ForData`（或等价命名），仅保存结构化参数（条件、次数、标签名等）。
2. **定义语法糖执行器**
   - 新增 `IfNode` / `ForNode`，内部不直接控制流程，仅负责产出跳转决策。
3. **预编译为 Jump/Label**
   - 在序列启动前预扫描语法糖节点，将其转换为 `LabelData` + `JumpData` 组合。
   - 保留原始语法糖节点用于编辑展示，但执行阶段只走预编译后的索引跳转。
4. **统一跳转内核**
   - Runner 仍只识别 `LabelData` / `JumpData`，保持唯一跳转通道。
5. **安全与调试**
   - 预编译时输出可读日志（语法糖 -> Jump 映射），便于排查跳转关系。

#### ⚠️ 当前策略调整（For 暂缓）
- **If 保留，但仅作为语法糖**：`IfData` + `IfNode` 不再承担“底层必备分支组件”的职责，而是由编辑器在创作阶段展开成一串 `Label + 条件跳转 + 结束跳转` 的结构；运行时只保留统一跳转内核。
- **条件能力的落点**：优先把条件能力并入跳转语义层，而不是新增 `Goto` 作为第二套核心跳转；如果后续条件系统足够稳定，可以再评估把条件表达抽成独立的 `ConditionData` / `ConditionGroupData` 数据层，但不引入节点嵌套。
- **多条件与取反**：单条件、条件取反、条件组合（All/Any）作为后续条件层的最低支持范围，避免 If 只能处理单一条件而失去实用性。
- **For 暂缓**：当前 `For` 的实现（按 `ContextVarKey` 计数循环）与原始期望不完全一致。
  - 原始期望更偏向“创建 For 命令后，自动在合适位置补 Label/Jump 结构”，该能力依赖编辑器编排与可视化反馈。
  - 该能力更适合在后续编辑器阶段（Phase 3/4）联动实现，而非仅在运行时层硬编码。
- **临时替代**：运行时循环行为统一建议通过 `If + Jump` 组合表达，保持跳转内核单一。

---

## 2.1 事件执行风险分析（运行时）

> 说明：本节用于梳理 Phase 2 运行时引擎在真实数据下的异常与风险。  
> **本阶段硬性要求仅为“死循环防护”落地**，其他项为建议性风险治理。

### A. 数据与映射类风险
1. **Data 无映射节点**
   - 现象：`ENSRegistry.GetNode(dataType)` 返回空，指令被跳过。
   - 影响：事件执行逻辑缺失，表现为“部分指令无效”。
   - 建议：保留警告日志，并在启动期输出映射覆盖率摘要。

2. **Label 重名覆盖**
   - 现象：预扫描时同名标签后者覆盖前者。
   - 影响：Jump 目标偏移，导致错误分支。
   - 建议：预扫描阶段检测重名并输出错误级日志（可选中断加载）。

3. **Jump 指向不存在标签**
   - 现象：`JumpToLabel` 查不到目标。
   - 影响：流程不中断但分支失效，逻辑偏差隐蔽。
   - 建议：记录上下文（序列名/索引/标签名），并统计告警次数。

### B. 执行流类风险
1. **同步节点形成高频自旋**
   - 现象：节点立即回调 `onComplete` + Jump 回退，单帧内可无限推进。
   - 影响：主线程卡死。
   - 建议：本阶段必须实现 Deadlock Guard（见 2.2）。

2. **嵌套触发导致 Runner 重入**
   - 现象：序列执行中再次触发 `StartSequence` 覆盖当前状态。
   - 影响：当前序列状态污染、回调丢失。
   - 建议：增加“运行中重入策略”（拒绝/排队/中断重启，三选一并文档化）。

3. **异常吞噬导致静默失败**
   - 现象：节点异常后继续推进，表面上流程“走完”。
   - 影响：业务状态不一致。
   - 建议：记录异常节点索引与类型，提供“异常即终止”的可选开关。

### C. 上下文与服务类风险
1. **首次触发服务未注册**
   - 现象：节点依赖服务解析失败。
   - 影响：节点降级执行或跳过。
   - 建议：在 `RegisterRequiredServices` 增加缺失服务聚合日志。

2. **上下文变量污染**
   - 现象：`Vars` 在长链条中被复用覆盖。
   - 影响：分支判定偏差。
   - 建议：关键分支变量（如 AllowEnter）在节点前后打印可选调试日志。

---

## 2.2 死循环防护（本阶段硬性要求）详细实现方案

### 目标
- 在不改变现有跳转语义的前提下，检测并中断“无等待/无异步进展”的循环。
- 保障主线程可恢复，不因错误编排导致整帧卡死。

### 判定原则（建议组合）
1. **步数上限防护**（硬阈值）
   - 单次 `StartSequence` 执行期间，累计执行指令次数超过 `MaxStepsPerSequence` 判定为异常。
2. **同帧高频回访防护**（强特征）
   - 在同一帧内，同一索引被访问次数超过 `MaxVisitsPerFramePerIndex` 判定为疑似死循环。
3. **无进展窗口防护**（可选增强）
   - 连续 N 次跳转均落在“已访问集合且无异步间隔”内，判定无进展。

### 具体改动点（按函数）

#### 1) `EventRunnerService` 字段新增
- 新增防护配置字段：
  - `private const int MaxStepsPerSequence = 10000;`
  - `private const int MaxVisitsPerFramePerIndex = 64;`
- 新增运行态字段：
  - `private int _stepCounter;`
  - `private int _currentFrame;`
  - `private readonly Dictionary<int, int> _indexVisitCounter = new();`

#### 2) `StartSequence(EventSequence sequence, EventNodeContext ctx, Action onComplete)`
- 在现有初始化逻辑中，额外重置：
  - `_stepCounter = 0;`
  - `_currentFrame = Time.frameCount;`
  - `_indexVisitCounter.Clear();`

#### 3) `ExecuteCurrentCommand()`
- 在读取 `_currentIndex` 后、实际执行前加入防护检查：
  - `if (!TryPassDeadlockGuard(_currentIndex)) { AbortSequenceByDeadlock(...); return; }`
- 防护通过后再执行节点。

#### 4) 新增 `TryPassDeadlockGuard(int index)`（建议私有函数）
- 职责：统一完成步数/同帧回访检测。
- 逻辑建议：
  1. `_stepCounter++`，超过上限直接返回 false。
  2. 若 `Time.frameCount != _currentFrame`：切帧，清空 `_indexVisitCounter`。
  3. 当前 `index` 访问计数 +1，超过阈值返回 false。
  4. 否则返回 true。

#### 5) 新增 `AbortSequenceByDeadlock(string reason)`（建议私有函数）
- 职责：统一中断策略。
- 行为建议：
  - 输出 `Debug.LogError`（包含：当前索引、步数、标签映射摘要、reason）。
  - 调用 `CompleteSequence()` 收口，避免状态悬挂。

#### 6) `CompleteSequence()`
- 在现有清理逻辑中补充：
  - `_indexVisitCounter.Clear();`
  - `_stepCounter = 0;`

### 日志规范（建议）
- 前缀统一：`[ENS-DeadlockGuard]`
- 最少包含字段：`sequenceHash/index/step/frame/label`。

### 验收标准（对齐 Timeline Step 4）
1. 构造 `Jump -> Label -> Jump` 的无等待循环序列。
2. 运行后可在限定阈值内触发中断，不出现主线程卡死。
3. 输出错误日志可定位到具体索引与标签。
4. 中断后 Runner 状态可恢复（后续序列可正常启动）。

---

## 2.3 多节点链并发执行的处理策略（新增）

> 说明：本节用于解决“序列 A 因异步节点挂起时，序列 B 被再次触发”的运行时问题。  
> 当前阶段不追求完整 RTOS 复杂调度，仅落地“可中断 + 可排队 + 可恢复”的最小可用机制。

### 问题定义
1. **单 Runner 状态被覆盖**
   - A 序列执行到异步节点（例如动画回调）后未完成，期间触发 B 序列。
   - 若 Runner 直接覆盖 `_currentSequence/_currentIndex/_onSequenceComplete`，A 回调归来时会污染 B 或直接丢失。
2. **异步完成回调晚到**
   - A 的 `onComplete` 在 B 运行中触发，若无“所属序列身份”校验，会推进错误序列。
3. **触发风暴**
   - 玩家短时间多次触发事件，若无队列与限流，Runner 会频繁重入或拒绝策略不一致。
### 【待删除-已验证存在问题】旧方案标记

### 目标边界（Phase 2）
1. 不做复杂优先级抢占，不引入完整任务调度器。
2. 支持三种基础能力：
   - **阻塞态释放**：当前序列在异步等待期间不锁死整个 Runner。
   - **就绪队列**：新触发序列进入队列，按 FIFO 执行。
   - **中断安全**：过期回调不会推进当前运行序列。
> 以下逻辑已验证存在结构性问题：把 `Running/Waiting` 作为 **Runner 全局状态**，而非“节点链（任务）状态”。
> 这些文本暂时保留用于追溯，后续应整体删除，避免误导。

### 建议状态模型（简化版）
1. `Running`：当前正在推进指令。
2. `Waiting`：当前序列等待异步节点回调。
3. `Ready`：排队待执行序列。
4. `Idle`：无运行序列且队列为空。
1. **问题点 A：状态归属错误**
   - 旧逻辑把 `Waiting` 写在 Runner 上，导致当前活动链路挂起时，Runner 不能正确切到其他就绪链路执行。
2. **问题点 B：执行上下文是全局裸字段**
   - `_currentSequence/_currentIndex/_labelMap/_stepCounter` 为全局单份，无法支持多个链路独立推进。
3. **问题点 C：阻塞链路无独立容器**
   - 异步挂起链路没有专门容器（BlockedRuns）持有，恢复路径与调度路径耦合在单活动链路模型中。

---

### ✅ 新定案：任务级状态调度（替代旧方案）

#### 1) 核心原则
1. Runner 只表示“是否有可运行任务”，即 `Idle / Busy` 两态（可由是否存在活动请求推导，不强依赖枚举）。
2. 任务（节点链）拥有自己的状态：`Ready / Running / Blocked / Completed / Aborted`。
3. 异步节点声明的是“当前任务将进入 Blocked”，不是 Runner 进入 Blocked。

#### 2) 必要数据结构
1. `Queue<SequenceRunRequest> _readyQueue`：就绪任务队列（FIFO）。
2. `Dictionary<long, SequenceRunRequest> _blockedRuns`：阻塞任务容器（按 runId 索引）。
3. `SequenceRunRequest _activeRun`：当前运行任务。
4. `long _nextRunId`：任务唯一标识。

`SequenceRunRequest` 需要从“轻量请求”提升为“完整任务上下文”，至少包含：
- 基础：`sequence/context/onComplete/runId/startFrame/state`
- 执行现场：`currentIndex`
- 跳转缓存：`labelMap`
- 防护计数：`stepCounter/currentFrame/indexVisitCounter`
- 提示防呆：`syncGuardStartTime/syncGuardMeta`

#### 3) 调度主流程（定案）
1. **StartSequence**
   - 创建任务并置 `Ready`。
   - 若当前无活动任务：立即拉起执行。
   - 否则入 `_readyQueue`。
2. **执行节点（ExecuteCurrentCommand）**
   - 仅操作 `_activeRun` 的任务级字段。
   - 若节点提示 `AsyncBlocking`：
     - 将 `_activeRun.state = Blocked`。
     - 移入 `_blockedRuns[runId]`。
     - 置空 `_activeRun`。
     - 立即调度下一条 `Ready` 任务。
3. **异步回调到达**
   - 用 `runId` 在 `_blockedRuns` 中定位原任务。
   - 定位成功：移出阻塞容器 -> 置 `Ready` -> 入 `_readyQueue`。
   - 定位失败：视为过期回调，仅告警。
4. **任务完成/中断**
   - `Completed/Aborted` 后释放任务资源。
   - 调度器继续拉起下一条就绪任务。

#### 4) 节点阻塞态上报机制（继续保留）
1. 继续采用 Node 侧显式声明：`IRunnerExecutionHintProvider` + `RunnerExecutionHint`。
2. `SyncImmediate`：任务保持 `Running`，同调用栈推进。
3. `AsyncBlocking`：任务转入 `Blocked`（不是 Runner 转 Waiting）。

#### 5) 轻量防呆（继续保留）
1. `SyncImmediate` 超时告警继续保留（`[ENS-ExecutionHintGuard]`）。
2. 该告警改为记录“任务级 runId + 任务索引”，不依赖全局活动字段。
3. 仅告警，不自动改写声明，不自动中断任务。

#### 6) 函数级改造映射（落地指引）
1. `StartSequence(...)`
   - 由“Runner状态分支”改为“创建任务并入队/激活”。
2. `ActivateRun(...)`
   - 仅装配 `_activeRun`，不复制到全局裸字段。
3. `ExecuteCurrentCommand()`
   - 全部读取/写入 `_activeRun` 内字段。
   - 遇 `AsyncBlocking` 时执行“入阻塞容器 + 让出执行位”。
4. `OnNodeComplete(runId)`（建议新增）
   - 统一处理回调归来逻辑：
     - 若来自 Blocked -> 重新入就绪队列。
     - 若来自 Active -> 正常推进。
5. `CompleteSequence()` / `AbortSequenceByDeadlock()`
   - 仅结束当前任务并触发下一次调度。
6. `TryDequeueAndRunNext()`
   - 成为唯一调度入口，保证调度一致性。

#### 7) 验收场景（替代旧验收）
1. A 执行到 `AsyncBlocking` 节点后进入 `Blocked`，Runner 立即可执行 B。
2. A 的异步回调到达后，A 进入 `Ready`，按队列规则继续执行。
3. 过期回调不会推进当前活动任务。
4. 死循环防护在任务级字段下仍有效（互不污染）。

#### 8) 阶段结论
1. 旧并发文本已被验证为有问题，必须在完成替换后整体删除。
2. Phase 2 并发方案以“任务级状态 + 阻塞容器 + 就绪队列”作为唯一正确实现方向。

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
| ✅ | **Step 1** | **容器与注册表搭建**<br>创建 `EventSequence` 以及 `ENSRegistry` 工厂。 | 能够在此处通过 `data.GetType()` 正确获得单一对应的、无状态的 `ENSNode` 享元实例。 |
| ✅ | **Step 2** | **主导引警 IP 循环重构**<br>重写 `EventRunnerService`，植入 `currentIndex` 和 `StartSequence()` 的 `List` 循环读取流程。预测试自动 `currentIndex++` 逻辑。 | `EventRunnerService` 可以在空白场景中接收一个自建的含有多个 Action Data 的 Sequence 并全自动按序跑完。 |
| ✅ | **Step 3** | **汇编指令实装 (Label & Jump)**<br>添加 `LabelData/Node` 与 `JumpData/Node`，编写预扫描构建 `_labelMap` 的逻辑。 | 在测试 Sequence 中设置 `Jump`，引擎能够正确根据 Label 名称跳过特定 Action 执行。 |
| ✅ | **Step 4** | **无限循环(Deadlock)安全防御**<br>为引擎的 Jump 机制加入安全锁。 | 当检测到无延时或无异步操作的死循环式 `Jump` 时（防卡死），直接中断 `EventRunnerService` 的循环并抛出严重警告报错。 |
| ✅ | **Step 5** | **多序列并发与中断机制**<br>引入任务级状态、阻塞容器与就绪队列。 | A 阻塞时 B 可执行；A 回调归来后可继续；过期回调不会污染活动序列。 |

*注：本阶段将奠定新版 ENS 的全部运行时基础。完成本阶段后，游戏内的逻辑层已经可以完全跑通。`If/For` 语法糖在本阶段收尾通过“预编译到 Jump”的方式追加。*

---

## 5. 阶段总结与下一步准入条件

### ✅ 已完成
1. `EventSequence` 容器与 `ENSRegistry` 派发链路已落地。
2. Runner 已切换到 IP 驱动（`StartSequence` / `RunSequenceAndWait`）。
3. `Label/Jump` 跳转核心已落地并可执行。
4. `If` 语法糖入口已保留（基于条件节点 + Jump）。
5. 死循环防护已完成并验证通过。
6. 并发调度、过期回调防护已完成并验证通过。
7. 旧 Flow 控制节点本体已清除，相关文件仅剩空文件占位，可视为历史痕迹。

### ⏸️ 策略性暂缓
1. `For` 语法糖暂缓：当前运行时循环实现与编辑器期望（自动补 Label/Jump）不一致，后移至编辑器阶段联动处理。
2. `If` 的内部条件编辑器暂缓：Tri 只能处理第一层多态列表，`If` 内嵌 `Condition` 的可视化编辑必须转入 Phase 3 自定义编辑器。

### ❗本阶段剩余硬性要求
1. 无新增运行时阻断项；当前主要剩余工作为文档收口与 Phase 3 编辑器链路规划。
2. 清理旧 Flow 控制节点与过时文档描述，确保 Phase 2 收尾状态与代码一致。

### ➡️ 进入下一阶段前的最低条件
1. Phase 2 运行时验收全部通过并记录完成状态。
2. 指导文件与代码状态一致（For 暂缓、If 保留、Jump 核心稳定）。
3. `If` 的嵌套条件编辑需求进入 Phase 3 自定义编辑器设计。

### 📌 最终结论
- Phase 2 的运行时底座已完成收尾。
- 复杂控制流的可视化编排与嵌套条件编辑，应进入 Phase 3。
- 旧控制流核心已移除；剩余历史方案仅作为文档追溯材料保留。
