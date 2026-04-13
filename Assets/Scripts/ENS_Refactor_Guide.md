# ENS (EventNodeSystem) 全面重构指导手册

## 1. 重构背景与整体目标
- **痛点**：当前由于 `Action` 与 `Flow` 节点将“指令行为(逻辑)”和“实例参数(数据)”强耦合在 `ScriptableObject` 中，造成海量不可复用的 SO 资源，导致“SO爆炸”，维护成本极高。
- **解法**：**享元模式 (Flyweight) + 纯数据配置**。SO 退化为无状态的执行逻辑模板（指令解释器）；参数提取为 `[Serializable]` 普通类并打上 `[SerializeReference]` 标签，直接存活在触发器组件的 `List` 中。
- **编辑器形态**：**Tri-Inspector (底层多态支持) + UI Toolkit (自研展现层)**，最终复刻类似 RPGMaker 的【左侧摘要列表 + 右侧详细修改】的高效事件页机制。

### 1.1 架构比对与设计思路（新旧对照）

| 旧架构 (SO-Based) | 新架构 (Data-Driven Event Page) |
| :--- | :--- |
| Node_Dialogue_001.asset (SO) -> 包含 { text="你好", nextNode=Node_002 } | EventCommandTemplate (SO) -> 提供 Execute(Param) 逻辑 (单例/享元模式) |
| Node_GiveItem_002.asset (SO) -> 包含 { itemId="5", count=1, nextNode=Node_003 }| BaseNodeData (Class) -> [Serializable] 纯数据基类 |
| | EventSequence (Class) -> 包含 [SerializeReference] List<BaseNodeData> |

*所有具体的行为表现完全由 `EventSequence` 列表实例驱动，节点模板仅作工厂/策略分发。*

---

## 2. 阶段任务与文件级修改明细

### Phase 1: 底层数据协议剥离 (Data Model & Node Interface)
**目标**：将所有在 `.asset` 里的字段（上下文数据）切分到纯 C# 类中，确立“纯数据基类”和“无状态逻辑节点”的边界。

*   **新增基类: `BaseNodeData.cs`**
    *   **逻辑**：所有节点参数的共有基类。包含 `[Serializable]` 标签。
    *   **核心方法**：需包含 `public abstract string GetSummary();` 方法。用于在编辑器 UI 列表中提供单行短文本（如 `"◆ 播放对话: 勇者，你来了"`），极大提升可读性。
*   **修改文件: `ActionNode` 和 `FlowNode` 的各自基类 (或 `ENSNode.cs`)**
    *   **逻辑**：抹除所有与特定事件上下文相关的字段。
    *   **接口签名更新**：从 `public abstract void Execute(EventRunnerService runner);` 改为 `public abstract void Execute(BaseNodeData data, EventRunnerService runner);`。
*   **修改文件: 非对话类 Action 节点**
    *   **逻辑**：剥离出内部字段并迁移到对应的 `XXXData`，以避免为一次性参数创建 SO 资源。
    *   **⚠️ 困难点 (状态隔离)**：如果部分旧节点在 SO 里存储了**运行期状态**（例如计时器 `float timer` 或 `bool isTriggered`），**绝对不能**将其提取到 `XXXData` 中！ `XXXData` 是只读的配置数据。运行时状态必须记录在 `EventRunnerService` 的缓存上下文中。
*   **停止维护: 对话类节点**
    *   **逻辑**：对话相关节点不再进入本次数据抽离流程，避免为即将替换的系统继续投入维护成本。

### Phase 2: 执行流与分支逻辑重构 (Execution & Flow Control)
**目标**：打破原有依靠“SO 互相引用/连线”的树状图执行流，拥抱“指令列表+指针跳转”的线性数组执行流。

*   **新增序列化容器: `EventSequence.cs`**
    *   **逻辑**：用来作为挂载在实体（如 NPC、事件触发器）上的参数容器。
    *   **核心字段**：`[SerializeReference] public List<BaseNodeData> commands = new();`。这是整个 ENS 的数据核心枢纽。
*   **修改文件: `EventRunnerService.cs` (执行引警核)**
    *   **核心逻辑**：加入“指令指针 (Instruction Pointer, IP)” 概念。Runner 开始时拿到 `EventSequence`，用一个 `int currentIndex` 依次读取 `List` 中的数据并结合享元 SO 模板执行。
*   **删除文件: `SequenceFlowNode.cs`、`ChoiceDialogueNode.cs` 等旧分支控制节点**
    *   **逻辑**：旧的树状引用方式与扁平化执行模型冲突，保留会导致维护双轨并增加迁移成本。
*   **新增节点: `LabelData` 与 `JumpData`**
    *   **逻辑**：通过统一的 Goto 底层指令替代旧的节点连线分支机制，实现线性序列中的跳转控制。
    *   **⚠️ 困难点 (分支心智模型)**：从图模型降维到表模型时，开发者需要时间适应“跳转”或“缩进”。在解析时需要严密测试跳转逻辑是否会死循环。
*   **结构化语法糖追加策略**
    *   **逻辑**：`If/For` 以 Goto 为统一底层，放在 Phase 2 收尾后追加，优先确保 `Label/Jump` 稳定可靠。

### Phase 3: 多态底层与外部插件引入 (Polymorphic Foundation)
**目标**：解决 Unity 原生对于含有 `[SerializeReference]` 的泛型列表无法在 Inspector 中选择具体实例化子类型的痛点。

*   **修改操作**：导入 **Tri-Inspector**。
*   **代码修改**：在 `EventSequence.cs` 中，为 `commands` 字段打上 Tri-Inspector 提供的高级特性（如 `[ListDrawerSettings]`, `[SerializeReference]` 或其等价特性），使其能够通过高级下拉菜单自动选择 `ModifyItemData`、`PlaySfxData` 等子类并实例化。
    *   *目的*：有了 Tri 兜底，右侧参数面板的绘制我们就不需要手写任何 `GUI.Layout` 或反射逻辑了。
*   **阶段定位**：Phase 3 聚焦于 **Inspector 内的下拉选择与字段展开**，确保数据可被创建和编辑。

### Phase 4: RPGMaker 风格事件页开发 (Editor Wizardry)
**目标**：告别 Inspector 的无限折叠菜单地狱，打造“基于 UI Toolkit 的终极沉浸式事件编辑器窗口”。
*   **阶段定位**：Phase 4 聚焦于 **独立编辑窗口的交互体验**，实现“左侧摘要列表 + 右侧详细修改”的完整事件页工作流。

#### 4.1 编辑器选型结论
为了实现 **“左侧摘要列表 + 右侧详细修改”** 的完美体验，不能仅依赖 Inspector 插件（它们只能改善字段绘制，无法解决长列表折叠地狱）。最终方案确定为：

**【自定义 UI Toolkit 窗口】 + 【轻量级多态渲染插件（Tri-Inspector）】 的混合双打模式。**

#### 4.2 具体实现路线
*   **新增文件: `EventPageEditorWindow.cs` (位于 Editor 目录)**
    *   **左侧列表 (基于 ListView)**：读取当前选中的 `EventSequence`，绑定给 UI Toolkit 的 ListView。在回调中，每行**仅调用 `GetSummary()` 显示单行文本**，达到清爽无比的效果。
    *   **右侧详情 (基于 Tri-Inspector 适配)**：当在 ListView 点击了特定的 `BaseNodeData` 时，获取该对象在 `SerializedObject` 队列中的 `SerializedProperty`。利用 `PropertyField` 将其渲染在右侧。此时 Tri-Inspector 会自动美化右侧参数面板的显示。
    *   **⚠️ 困难点 (数据绑定)**：在 UI Toolkit 中实现列表选择事件与右侧 `SerializedProperty` 界面的双向绑定比较绕，需要阅读最新的 `PropertyBinding` 机制，或者手动在 `onSelectionChange` 中清空右侧容器并重新 `Add(new PropertyField(selectedProperty))`。
    *   **渲染对接 (If/For)**：为 `If/For` 语法糖节点提供缩进、色块或图标区分，避免在列表中与普通指令混淆。

### Phase 5: 旧资产平滑过渡 (Data Migration)
**目标**：将所有在硬盘和场景里的老 SO 全部解剖，转移进新版结构，清理战场。
*   **阶段定位**：该阶段以工具化迁移为主，整体实现量较小，但对数据完整性要求极高。

*   **新增文件: `ENSMigrationTool.cs` (位于 Editor 目录)**
    *   **步骤 1**：扫描 `Resources` 或指定目录，收集所有的 `ActionNode/FlowNode` SO。
    *   **步骤 2**：扫描场景里包含 `EventRunnerService` 或旧调用器的组件。
    *   **步骤 3 (脚本映射)**：写一个庞大的 switch-case，如果是 `OldShowDialogue`，就实例化一个新的 `ShowDialogueData`，把 `old.text` 赋值给 `new.text`。
    *   **步骤 4**：将所有新生成的 Data 按执行图顺序展平，装填进全新的 `EventSequence` 中并挂载回场景。
    *   **步骤 5**：将所有僵尸 SO 标记为待删除，开发者人工核对测试无误后全部 `AssetDatabase.DeleteAsset`。
    *   **⚠️ 困难点 (安全性)**：如果旧的连线图过于复杂且有往复循环连线，如何自动展平成带有 Label/Jump 的线性结构是算法难点。若实在无法自动，可能需要抛弃全自动迁移，而是仅保留辅助工具，由策划/程序手动照着旧图重新连设新列表。

---

## 3. 进度追踪 (Progress Tracker)

**当前阶段**：初期调研与可行性评估

*   **2026-4-13** : 📝 完成方案初步设计，确认核心痛点并敲定“SO享元 + Serializable实例”的设计模式。

---

## 4. 核心评估与开发守则复盘
1. **彻底无状态**：再次强调，`XXXData` 对象会被一直留在 `List` 里被序列化，绝对不能在里面保存 `int currentDialogueIndex` 等运行时数据。
2. **渐进式重构**：先拿最基础的 `ModifyItem` 和 `SequenceFlow` 在空白测试场景里走通 **Phase 1 -> Phase 4** 的完整形态，再铺开重制其他所有节点。