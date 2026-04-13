# ENS (EventNodeSystem) 重构 Phase 3: Tri-Inspector 接入与编辑器数据链路打通

## 1. 阶段概述与边界澄清
本阶段专注于 **Tri-Inspector 与 ENS 数据模型的集成**，目标是让 `[SerializeReference]` 的多态数据在 Inspector 中可编辑、可实例化、可维护。此阶段不引入 UI Toolkit 的事件页窗口，不调整运行时逻辑。
本阶段的重心是 **Inspector 下拉创建与字段展开**，与 Phase 4 的 **独立窗口事件页交互** 明确分离。

---

## 2. 核心增改特性与解决的问题
- 技术选型明确为 **Tri-Inspector**。
- 以 **`[SerializeReference]` 多态可视化** 为第一优先级。
- 以 **Tri 与 ENS 数据模型的集成点** 为落地重点。

### 特性 1：多态列表实例化菜单
- **要解决的问题**：Unity 原生 Inspector 无法为 `[SerializeReference]` 列表提供稳定的子类实例化入口，导致 `EventSequence.Commands` 无法正常添加 `BaseNodeData` 子类。
- **设计逻辑**：通过 Tri-Inspector 的多态下拉菜单能力，让 `Commands` 列表可以直接创建 `ModifyItemData`、`PlaySfxData` 等子类实例。

### 特性 2：字段绘制统一与布局稳定
- **要解决的问题**：不同 `BaseNodeData` 子类字段结构差异大，原生 Inspector 展示不稳定，且编辑体验断裂。
- **设计逻辑**：使用 Tri 的布局与绘制能力让所有子类字段保持一致的呈现风格，便于后续 UI Toolkit 绑定时复用同一套 `PropertyField` 逻辑。

### 特性 3：Tri 与 ENS 的集成点定义
- **要解决的问题**：Tri 需要明确的落点，否则会变成纯插件引入而无法真正作用于 ENS。
- **设计逻辑**：明确以下三处为唯一集成点：
  - `EventSequence.Commands` 的多态列表渲染。
  - `BaseNodeData` 子类字段的 Inspector 绘制。
  - 后续 UI Toolkit 的右侧详情面板复用 Tri 渲染逻辑。

---

## 3. 具体涉及的文件修改清单

*所有路径前缀基于 `Assets/Scripts/Modules/EventNodeSystem/`*

### 📝 1. 新增/创建的文件
| 文件路径 | 修改类型 | 说明与职责 |
| :--- | :--- | :--- |
| `Editor/TriInspectorBridge.cs` | **新增** | 统一管理 Tri 与 ENS 的集成特性，集中放置 Tri 的特性标注或辅助逻辑。 |

### 🛠️ 2. 修改与重构的文件
| 文件路径 | 修改类型 | 说明与职责 |
| :--- | :--- | :--- |
| `Core/EventSequence.cs` | **修改** | 为 `Commands` 添加 Tri 的列表绘制特性，确保多态下拉可用。 |
| `Core/BaseNodeData.cs` | **修改** | 允许 Tri 更稳定识别子类，保持 `GetSummary()` 作为列表摘要入口。 |
| `Nodes/Action/Data/XXXData.cs` | **修改** | 为各子类数据补齐更清晰的字段命名与默认值，提升 Tri Inspector 展示效果。 |
| `Nodes/Flow/Data/LabelData.cs` | **修改** | 确保最基础的跳转数据类也能在 Inspector 中快速设置标签名。 |
| `Nodes/Flow/Data/JumpData.cs` | **修改** | 确保目标标签字段命名统一，便于后续 UI Toolkit 中的字段绑定。 |

### 🚫 3. 不在本阶段处理的内容
- 不实现 UI Toolkit 的事件页窗口。
- 不调整运行时跳转逻辑与执行引擎。

---

## 4. 进度追踪与开发时间线 (Timeline)

| 状态 | 预期时间 | 检查点 (Checkpoint) / 任务描述 | 达成判定标准 |
| :---: | :---: | :--- | :--- |
| 🔄 | **Step 1** | **Tri 插件接入与基础验证**<br>导入 Tri-Inspector 并确认不影响现有编译。 | 在 Inspector 中能正常打开 `EventSequence` 所在对象。 |
| 🔄 | **Step 2** | **多态列表可用性打通**<br>为 `EventSequence.Commands` 添加 Tri 列表特性。 | `Commands` 可直接创建 `BaseNodeData` 子类实例。 |
| 🔄 | **Step 3** | **子类字段展示优化**<br>逐个检查核心 `XXXData` 类的字段命名与默认值，保证 Tri 展示一致性。 | Inspector 中不同 Data 的字段能清晰区分，无重复或歧义。 |
| 🔄 | **Step 4** | **集成点封装与一致性确认**<br>整理 Tri 与 ENS 的集成逻辑入口。 | 后续 Phase 4 能直接复用 Tri 绘制逻辑，不再新增兼容层。 |

*注：本阶段是 ENS 编辑器体验提升的最低门槛，完成后才允许开始 UI Toolkit 事件页窗口的开发。*
