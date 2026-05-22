# ENS (EventNodeSystem) 重构 Phase 4: UI Toolkit 事件页编辑器窗口

## 1. 阶段概述与边界澄清
本阶段专注于 **独立事件页编辑器窗口** 的设计与实现，目标是复刻 RPGMaker 的事件页工作流：左侧指令摘要列表，右侧参数详情编辑。此阶段不修改运行时执行逻辑。

本阶段的重心是 **独立窗口交互与可视化编排**，与 Phase 3 的 **Inspector 下拉创建与字段展开** 明确分离。

### 阶段性标签
- `P4-EventPageWindow`
- `P4-ListViewSummary`
- `P4-DetailPanel`
- `P4-IfForRender`

---

## Phase3 状态摘要（供 Phase4 使用）
为保障 UI Toolkit 窗口开发的对齐与兼容，下面汇总 Phase3 已落地的运行时与数据约定（仅保留现行方案，历史方案不再迁移）。UI 实现应基于这些约定做最小假设并提供友好回退：

- Tri-Inspector 已集成并在项目中可用；Inspector 下的 `EventSequence.commands` 列表可通过 Tri 多态下拉创建与展开顶层 `BaseNodeData` 子类。
- 左侧摘要列表（ListView）应使用 `BaseNodeData.GetSummary()` 输出作为一行摘要文本，GetSummary 已在常用 Data 中补齐以便快速识别指令语义。
- 运行时控制流原语不变：Jump/Label 为唯一底层跳转实现，EventRunnerService 在运行时解析并执行 Jump/Label。UI 不应假设 If/For 为运行时容器节点。
- Jump 与条件的现行约定（Phase3 定案）：
  - Jump 通过 lookahead 只捕捉紧邻的下一个节点（`Commands[index + 1]`）作为条件；若后继为 `Condition` 类型则被视作该 Jump 的条件数据并由 Jump 评估；否则视为无条件跳转。
  - 被 Jump 捕捉的条件节点在运行时不作为独立指令执行（ConditionNode.Execute 已改为直接完成）；条件评估入口由 Jump 调用 ConditionNode.Evaluate(...)。
  - JumpData 已新增 `alwaysJump` 布尔标志，用于在任何情况下强制跳转；UI 需要在详情面板暴露该字段并在摘要中提示（summary 已包含标记）。
- 条件数据的表达能力：常用条件 Data（示例：PlayerHasAttributeData / PlayerHasItemData）已扩展 `ComparisonMode`（>、>=、<、<=、==、!=），UI 需要将该枚举以可读标签展示并在详情面板支持编辑。
- If/For 的定位：If/For 不再作为运行时节点，而是编辑器层面的语法糖或模板，最终应在保存/预处理阶段展开为 Label + 条件 Jump 的平铺结构。Phase4 的事件页窗口对 If/For 的渲染应按“语法糖/区块”展示（缩进/颜色/图标），但不依赖其为运行时容器。
- 编辑器校验与兼容性考虑：
  - 需要在 UI 中提供静态验证入口（或快捷按钮），检查 Jump 指向的 Label 是否存在、Label 重名、Jump 后继非 Condition 的情况等，并把定位信息反馈给用户。
  - Tri 可能在不同环境下表现不一致，建议在窗口中加入降级/回退路径（若 Tri 不可用，使用 SerializedProperty + PropertyField 的默认回退绘制）。

以上内容为 Phase4 实现 UI 时应依赖的最小运行时/数据契约，UI 设计应以此为准并避免对旧的历史方案作进一步假设。
## 2. 核心增改特性与解决的问题
- 事件页以 **UI Toolkit** 为核心实现技术。
- 继续复用 Phase 3 已建立的 **Tri-Inspector 字段绘制能力**。
- 窗口专注于 “列表摘要 + 详情编辑” 的流式工作流。

### 特性 1：事件页独立窗口入口
- **要解决的问题**：Inspector 模式在长列表时可读性极差，无法胜任大规模事件编排。
- **设计逻辑**：新增可从菜单或组件按钮打开的 `EventPageEditorWindow`，并聚焦当前选中的 `EventSequence` 数据。

### 特性 2：左侧指令摘要列表
- **要解决的问题**：在大量节点中缺乏快速定位能力，无法像 RPGMaker 一样一眼扫过事件流。
- **设计逻辑**：使用 `ListView` 显示 `BaseNodeData.GetSummary()` 返回的单行文本，支持选中、高亮与顺序调整。

### 特性 3：右侧详情面板与字段复用
- **要解决的问题**：多态数据字段复杂且变动频繁，自绘 UI 成本高。
- **设计逻辑**：右侧采用 `PropertyField` 绑定选中元素的 `SerializedProperty`，并让 Tri 继续负责字段渲染与布局。

### 特性 4：列表操作与数据一致性
- **要解决的问题**：新增、删除、移动指令时数据容易错位，窗口状态易失效。
- **设计逻辑**：封装统一的列表操作入口，所有操作都围绕 `SerializedObject` 的数组变更，确保撤销/重做与序列化稳定。

### 特性 5：`If/For` 语法糖的渲染对接
- **要解决的问题**：`If/For` 已在 Phase 2 追加，编辑器需要提供直观的缩进与区块表现，避免把结构化指令误读为普通指令行。
- **设计逻辑**：在 `ListView` 渲染层识别 `If/For` 语法糖节点，提供缩进、颜色或图标区分，并与 `GetSummary()` 输出保持一致。

---

## 3. 具体涉及的文件修改清单

*所有路径前缀基于 `Assets/Scripts/Modules/EventNodeSystem/`*

### 📝 1. 新增/创建的文件
| 文件路径 | 修改类型 | 说明与职责 |
| :--- | :--- | :--- |
| `Editor/EventPageEditorWindow.cs` | **新增** | UI Toolkit 编辑器窗口主体，负责左侧列表与右侧详情面板布局。 |
| `Editor/EventPageEditorWindow.uxml` | **新增** | 事件页窗口的 UI 结构定义。 |
| `Editor/EventPageEditorWindow.uss` | **新增** | 事件页窗口的样式定义与视觉规范。 |

### 🛠️ 2. 修改与重构的文件
| 文件路径 | 修改类型 | 说明与职责 |
| :--- | :--- | :--- |
| `Core/EventSequence.cs` | **修改** | 增加编辑器专用的入口点或菜单按钮挂接入口。 |
| `Editor/TriInspectorBridge.cs` | **修改** | 确保右侧详情面板复用 Tri 的字段绘制能力。 |
| `Core/BaseNodeData.cs` | **修改** | 保持 `GetSummary()` 稳定输出，确保左侧列表摘要一致。 |

### 🚫 3. 不在本阶段处理的内容
- 不改变 `EventRunnerService` 的运行时行为。
- 不调整 `Label/Jump` 的执行机制。

---

## 4. 进度追踪与开发时间线 (Timeline)

| 状态 | 预期时间 | 检查点 (Checkpoint) / 任务描述 | 达成判定标准 |
| :---: | :---: | :--- | :--- |
| 🔄 | **Step 1** | **窗口骨架搭建**<br>完成 `EventPageEditorWindow` 的基础布局与 UXML/USS 结构。 | 窗口可以在菜单中打开并显示基础分栏结构。 |
| 🔄 | **Step 2** | **左侧摘要列表接入**<br>绑定 `EventSequence` 的 `Commands` 到 `ListView`。 | 列表能正确显示 `GetSummary()` 内容并支持选中。 |
| 🔄 | **Step 3** | **右侧详情面板渲染**<br>绑定选中元素的 `SerializedProperty` 到 `PropertyField`。 | 选中列表项时右侧正确刷新字段。 |
| 🔄 | **Step 4** | **列表操作与数据稳定性**<br>实现新增、删除、移动、撤销/重做的稳定流程。 | 列表结构与序列化数据一致，操作后无丢失或错位。 |

*注：本阶段完成后，事件页编辑体验进入可用状态，可进入 Phase 5 的资产迁移与内容规模化验证。*
