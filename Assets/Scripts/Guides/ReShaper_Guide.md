# 🎯 Mota 项目模块命名重构与命名空间引入完整执行计划

本计划用于在开始正式整理前，完整定义“从改动到验收”的实施路径。目标不是单纯把所有脚本包进 namespace，而是把当前项目的物理目录、逻辑模块、历史旧名、Editor/Runtime 边界、Unity 序列化风险统一纳入同一套执行顺序中，确保重构可落地、可回滚、可验证。

## 一、总体目标

1. 为整个项目建立统一且可持续的命名空间体系。
2. 保持当前模块边界清晰，不破坏 `Core / Enemy / EventNodeSystem / EventSystem / Item / Map / Player / UI / YarnIntegration / DependencyInject` 的分层。
3. 优先处理高风险文件：集中定义文件、事件节点系统核心文件、历史旧名残留文件、跨模块公共契约、Editor 工具。
4. 每完成一批改动都进行编译验证，并对 Unity 序列化、SO、反射、注册表、Inspector 绑定做回归检查。
5. 最终消除 ReSharper 对“命名空间与文件夹路径不一致”的核心警告，并让后续新增文件有统一规范可遵循。

## 二、统一规则

### 2.1 根命名空间
- 建议根命名空间统一使用 `Mota`。
- 若后续团队约定不同，可在全计划落地前一次性替换，但不得在同一阶段内混用多个根命名空间。

### 2.2 模块命名空间
- 运行时代码建议采用：`Mota.Modules.<模块名>...`
- 或在团队更偏简洁时采用：`Mota.<模块名>...`
- 本计划默认保留 `Modules` 作为统一前缀，原因是当前目录结构本身就以 `Assets/Scripts/Modules` 为主干，映射最自然。

### 2.3 分层规则
- `DataDefine`：放数据定义、枚举、接口、数据结构、契约类。
- `Runtime`：放运行时服务、系统逻辑、管理器、行为节点、流程控制。
- `Editor`：放编辑器扩展、生成器、工具窗口、迁移脚本。
- `DataDefine/Context`、`Runtime/Nodes/...` 等子目录应继续映射为更深层 namespace，不做人为扁平化。

### 2.4 改名原则
- 先 namespace，后细分文件改名。
- 先公共契约，后实现类。
- 先低耦合文件，后高耦合文件。
- 先确认历史旧名是否仍被引用，再决定是否真正改名或仅改 namespace。

### 2.5 验收原则
- 编译通过。
- Unity Editor 可正常打开相关资源。
- Inspector 中序列化字段无明显丢失。
- 关键注册表、节点系统、事件系统、UI、地图、背包、战斗流程可正常运行。
- ReSharper 的 namespace 相关警告明显减少或消失。

## 三、改动顺序总览

本次重构采用“先规则、后基础、再核心、最后外围”的顺序：

1. 固定命名空间策略与文件映射规则。
2. 建立项目级文件分组与优先级。
3. 处理 `Core/DataDefine/Global` 集中定义文件。
4. 处理 `EventNodeSystem` 核心运行时与节点文件。
5. 处理历史命名与现行命名不一致的文件。
6. 处理跨模块公共契约与服务接口。
7. 处理 Editor 工具脚本。
8. 补全引用、using、显式 namespace 依赖。
9. 分批编译、回归、确认验收。

## 四、按文件顺序排列的执行清单

### 第 1 批：Core 基础定义文件
**目标**：先稳定项目中最基础、被最多模块引用的定义文件。

建议顺序：
1. `Assets/Scripts/Modules/Core/DataDefine/Global/GlobalTypeDefine.cs`
2. `Assets/Scripts/Modules/Core/DataDefine/Global/GlobalVariables/GlobalEventKey.cs`
3. `Assets/Scripts/Modules/Core/DataDefine/Global/GlobalVariables/IGlobalEventVariables.cs`
4. `Assets/Scripts/Modules/Core/DataDefine/Tile/BaseTile.cs`
5. `Assets/Scripts/Modules/Core/DataDefine/Tile/EventTile.cs`
6. `Assets/Scripts/Modules/Core/DataDefine/Tile/GroundTile.cs`
7. `Assets/Scripts/Modules/Core/DataDefine/Tile/ObstacleTile.cs`
8. `Assets/Scripts/Modules/Core/DataDefine/Units/AttributeUnit.cs`

处理要求：
- 先补 namespace。
- `GlobalTypeDefine.cs` 要重点检查是否应拆分。
- 若拆分，优先拆分枚举和多职责数据结构，避免单文件容纳过多概念。
- 这些文件涉及全局契约，改动后立即进行一次编译检查。

### 第 2 批：Core 运行时与全局服务
**目标**：让核心启动、计算、全局变量服务与 namespace 对齐。

建议顺序：
1. `Assets/Scripts/Modules/Core/Runtime/GameInitializationEntryPoint.cs`
2. `Assets/Scripts/Modules/Core/Runtime/Caculate/GoldRewardCaculate.cs`
3. `Assets/Scripts/Modules/Core/Runtime/GlobalVariables/GlobalEventVariablesInspector.cs`
4. `Assets/Scripts/Modules/Core/Runtime/GlobalVariables/GlobalEventVariablesService.cs`

处理要求：
- 统一命名空间。
- 检查是否存在依赖注入、事件中心、反射注册、资源加载路径依赖。
- 若有 `using` 大量缺失，先补同模块引用，再处理跨模块引用。

### 第 3 批：DependencyInject
**目标**：稳定装配入口与全局注入逻辑。

建议顺序：
1. `Assets/Scripts/Modules/DependencyInject/Runtime/DiBootstrap.cs`
2. `Assets/Scripts/Modules/DependencyInject/Runtime/GlobalInjection.cs`

处理要求：
- 命名空间必须稳定。
- 若存在按类型名或程序集扫描的逻辑，需要确认改 namespace 后仍能命中。

### 第 4 批：Enemy 模块
**目标**：为敌人数据与战斗逻辑建立一致命名体系。

建议顺序：
1. `Assets/Scripts/Modules/Enemy/DataDefine/EnemyData.cs`
2. `Assets/Scripts/Modules/Enemy/DataDefine/EnemyDatabase.cs`
3. `Assets/Scripts/Modules/Enemy/DataDefine/EnemyUnit.cs`
4. `Assets/Scripts/Modules/Enemy/Runtime/BattleManager.cs`

处理要求：
- `EnemyDatabase` 如果是 Unity 资源/数据容器，要重点检查序列化兼容性。
- 保持数据与战斗逻辑分层清晰。

### 第 5 批：EventNodeSystem 基础契约与注册层
**目标**：先稳定节点系统的基础对象与注册入口。

建议顺序：
1. `Assets/Scripts/Modules/EventNodeSystem/DataDefine/Context/ContextVarKey.cs`
2. `Assets/Scripts/Modules/EventNodeSystem/DataDefine/Context/EventNodeContext.cs`
3. `Assets/Scripts/Modules/EventNodeSystem/DataDefine/Context/ItemEventContext.cs`
4. `Assets/Scripts/Modules/EventNodeSystem/DataDefine/Context/TileEventContext.cs`
5. `Assets/Scripts/Modules/EventNodeSystem/DataDefine/Data/EventNodeTile.cs`
6. `Assets/Scripts/Modules/EventNodeSystem/DataDefine/Data/EventTileData.cs`
7. `Assets/Scripts/Modules/EventNodeSystem/DataDefine/Data/ModifyActionEnums.cs`
8. `Assets/Scripts/Modules/EventNodeSystem/DataDefine/IEventTileRegistry.cs`
9. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Core/BaseNodeData.cs`
10. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Core/EventNodeSystemRegistry.cs`
11. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Core/EventSequence.cs`
12. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Core/NodeMappingTableSO.cs`

处理要求：
- 这批文件是节点系统的基础层，必须先稳定。
- `NodeMappingTableSO` 要特别注意 Unity 序列化和资源引用。
- `IEventTileRegistry` 属于契约文件，应优先保证其 namespace 长期稳定。

### 第 6 批：EventNodeSystem 核心运行时与历史命名重点
**目标**：解决历史旧名残留与当前语义不一致的问题。

建议顺序：
1. `Assets/Scripts/Modules/EventNodeSystem/Runtime/EventTileManager.cs`
2. `Assets/Scripts/Modules/EventNodeSystem/Runtime/EventTileRegistry.cs`
3. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Runner/CoroutineRunner.cs`
4. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Runner/EventRunnerService.cs`
5. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Runner/IEventRunner.cs`
6. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Runner/IRunnerExecutionHintProvider.cs`
7. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Runner/RunnerExecutionHint.cs`

处理要求：
- `EventTileManager` 是明确的历史命名关注点，需先确认现行职责和旧名来源。
- 若类名、文件名、注释或注册逻辑存在旧称残留，应统一为现行名。
- 对外暴露接口先对齐，内部实现后续再整理。

### 第 7 批：EventNodeSystem 节点层
**目标**：把 Action / Condition / Flow 节点和数据文件的 namespace 全部对齐。

建议顺序：
1. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/EventNode.cs`
2. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/ActionNode.cs`
3. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/Data/ModifyAttributeData.cs`
4. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/Data/ModifyItemData.cs`
5. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/Data/PickaxeActionData.cs`
6. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/Data/PlayAnimationData.cs`
7. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/Data/RemoveTileData.cs`
8. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/Data/SetVariableData.cs`
9. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/Data/SwitchLayerData.cs`
10. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/Data/YarnDialogueData.cs`
11. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/ItemAction/ItemActionNode.cs`
12. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/ItemAction/PickaxeActionNode.cs`
13. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/ModifyAttribute.cs`
14. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/ModifyItem.cs`
15. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/SetVariable.cs`
16. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/SwitchLayer.cs`
17. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/TileAction/TileActionNode.cs`
18. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/TileAction/PlayAnimation.cs`
19. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/TileAction/RemoveTile.cs`
20. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Action/YarnDialogue.cs`
21. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Condition/ConditionNode.cs`
22. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Condition/CanDefeat.cs`
23. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Condition/Data/PlayerHasAttributeData.cs`
24. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Condition/Data/PlayerHasItemData.cs`
25. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Condition/PlayerHasAttribute.cs`
26. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Condition/PlayerHasItem.cs`
27. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Condition/TileConditionNode.cs`
28. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Flow/Data/ForData.cs`
29. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Flow/Data/IfData.cs`
30. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Flow/Data/JumpData.cs`
31. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Flow/Data/LabelData.cs`
32. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Flow/ForNode.cs`
33. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Flow/IfNode.cs`
34. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Flow/JumpNode.cs`
35. `Assets/Scripts/Modules/EventNodeSystem/Runtime/Nodes/Flow/LabelNode.cs`

处理要求：
- 文件与 namespace 必须严格一致。
- 深层目录不要手动扁平化，避免后续维护混乱。
- 修改后优先检查编译错误与节点注册链路是否完整。

### 第 8 批：EventSystem 公共事件契约
**目标**：统一事件参数命名空间，确保跨模块可见性稳定。

建议顺序：
1. `Assets/Scripts/Modules/EventSystem/DataDefine/EventArgs/AttributeEvents.cs`
2. `Assets/Scripts/Modules/EventSystem/DataDefine/EventArgs/BattleEvents.cs`
3. `Assets/Scripts/Modules/EventSystem/DataDefine/EventArgs/GridEvents.cs`
4. `Assets/Scripts/Modules/EventSystem/DataDefine/EventArgs/InventoryEvents.cs`
5. `Assets/Scripts/Modules/EventSystem/DataDefine/EventArgs/LayerSwitchEvents.cs`
6. `Assets/Scripts/Modules/EventSystem/DataDefine/EventArgs/PlayerMoveEvents.cs`
7. `Assets/Scripts/Modules/EventSystem/DataDefine/EventArgs/UIEvents.cs`
8. `Assets/Scripts/Modules/EventSystem/Runtime/EventCenter.cs`

处理要求：
- 这是典型跨模块契约层，namespace 不应频繁变化。
- `EventCenter` 应作为核心中枢单独验证。

### 第 9 批：Item 模块
**目标**：完成道具数据、使用逻辑与 monster book 子域的命名统一。

建议顺序：
1. `Assets/Scripts/Modules/Item/DataDefine/ItemData.cs`
2. `Assets/Scripts/Modules/Item/DataDefine/ItemDatabase.cs`
3. `Assets/Scripts/Modules/Item/DataDefine/ItemTypeLib.cs`
4. `Assets/Scripts/Modules/Item/DataDefine/ItemUnit.cs`
5. `Assets/Scripts/Modules/Item/Runtime/ItemUseHandler.cs`
6. `Assets/Scripts/Modules/Item/Runtime/MonsterBook/IMonsterBook.cs`
7. `Assets/Scripts/Modules/Item/Runtime/MonsterBook/MonsterBookService.cs`

处理要求：
- `MonsterBook` 子目录应保持独立 namespace。
- `IMonsterBook` 作为契约应优先稳定。

### 第 10 批：Map 模块
**目标**：完成地图与网格系统的 namespace 统一。

建议顺序：
1. `Assets/Scripts/Modules/Map/DataDefine/MapLayerInfo.cs`
2. `Assets/Scripts/Modules/Map/Runtime/GridManager.cs`
3. `Assets/Scripts/Modules/Map/Runtime/MapManager.cs`

处理要求：
- 确认与 `Core/DataDefine/Tile` 的类型引用正确。
- 若存在地图层与事件层共享类型，要提前检查 using。

### 第 11 批：Player 模块
**目标**：完成背包、移动、状态与属性相关文件的命名统一。

建议顺序：
1. `Assets/Scripts/Modules/Player/DataDefine/IInventoryService.cs`
2. `Assets/Scripts/Modules/Player/DataDefine/InventoryEntry.cs`
3. `Assets/Scripts/Modules/Player/DataDefine/PlayerAttribute.cs`
4. `Assets/Scripts/Modules/Player/DataDefine/PlayerState.cs`
5. `Assets/Scripts/Modules/Player/Runtime/Inventory/Input/InventoryUIInput.cs`
6. `Assets/Scripts/Modules/Player/Runtime/Inventory/PlayerInventory.cs`
7. `Assets/Scripts/Modules/Player/Runtime/Inventory/UI/InventorySlot.cs`
8. `Assets/Scripts/Modules/Player/Runtime/Inventory/UI/InventoryUI.cs`
9. `Assets/Scripts/Modules/Player/Runtime/Movement/MovementInputManager.cs`
10. `Assets/Scripts/Modules/Player/Runtime/Movement/PlayerAnimationController.cs`
11. `Assets/Scripts/Modules/Player/Runtime/Movement/PlayerMovement.cs`

处理要求：
- `IInventoryService` 是接口契约，必须与实现层明确分离。
- Inventory 与 Movement 两条子线分别处理，不要混改。

### 第 12 批：UI 模块
**目标**：统一 UI 数据层、输入层与表现层命名空间。

建议顺序：
1. `Assets/Scripts/Modules/UI/DataDefine/UIRootIdentifier.cs`
2. `Assets/Scripts/Modules/UI/DataDefine/UIRootType.cs`
3. `Assets/Scripts/Modules/UI/DataDefine/UIState.cs`
4. `Assets/Scripts/Modules/UI/Runtime/BaseUI.cs`
5. `Assets/Scripts/Modules/UI/Runtime/Input/UIInputF4Manager.cs`
6. `Assets/Scripts/Modules/UI/Runtime/Input/UIInputXManager.cs`
7. `Assets/Scripts/Modules/UI/Runtime/MonsterBar.cs`
8. `Assets/Scripts/Modules/UI/Runtime/MonsterBookUI.cs`
9. `Assets/Scripts/Modules/UI/Runtime/UIMain.cs`
10. `Assets/Scripts/Modules/UI/Runtime/UIManager.cs`
11. `Assets/Scripts/Modules/UI/Runtime/UIRootDatabase.cs`
12. `Assets/Scripts/Modules/UI/Runtime/UISideMenu.cs`

处理要求：
- UI 是高耦合模块，namespace 改动后容易暴露缺失引用。
- `UIRootDatabase` 和 UI 资源绑定要重点回归。

### 第 13 批：YarnIntegration 模块
**目标**：为对话桥接类建立稳定命名空间。

建议顺序：
1. `Assets/Scripts/Modules/YarnIntegration/Runtime/YarnRouteBridge.cs`

处理要求：
- 这是桥接层，命名要清晰反映跨系统职责。

### 第 14 批：Editor 工具脚本
**目标**：将编辑器脚本统一放入 Editor 命名空间，并避免与运行时冲突。

建议顺序：
1. `Assets/Editor/Brush/EventTileBrush.cs`
2. `Assets/Editor/EventNodeSystem/ENSRegistryGenerator.cs`
3. `Assets/Editor/EventNodeSystem/EventNodeNestedEditor.cs`
4. `Assets/Editor/EventNodeSystem/IfFlowNodeEditor.cs`
5. `Assets/Editor/EventNodeSystem/SequenceFlowNodeEditor.cs`
6. `Assets/Editor/MapLayerBoundsEditor.cs`
7. `Assets/Editor/MigrateYarnUI.cs`

处理要求：
- Editor 脚本单独 namespace。
- 工具类按用途拆分子命名空间。
- 确认编辑器扩展对运行时类型的引用仍可解析。

## 五、补引用与 using 的执行方式

namespace 引入完成后，执行如下修复顺序：

1. 先修复同文件夹、同模块内部引用。
2. 再修复跨模块引用。
3. 再清理冗余 using。
4. 最后检查是否存在全局命名空间遗留引用。

修复重点：
- `EventNodeSystem` 与 `Core` 的互相引用。
- `UI` 对 `Player`、`EventSystem`、`Map` 的引用。
- `Player` 对 `Core`、`EventSystem` 的引用。
- `Editor` 对运行时代码的引用。

## 六、拆分与重命名决策点

### 6.1 `GlobalTypeDefine.cs`
该文件必须在第一批重点判断是否拆分。建议拆分条件如下：
- 一个文件内承载多个领域概念，且不再共享同一演化频率。
- 某些枚举只被特定模块使用，不适合继续放在全局集合中。
- 拆分后可以更清楚表达职责边界。

如果拆分，优先按主题拆：
- 瓦片/地图相关
- 属性相关
- 楼梯/出生相关

### 6.2 `EventTileManager`
该文件必须先确认现行职责再改名：
- 如果只是历史遗留命名，则统一到现行名称。
- 如果职责已变更，则先写出新职责定义，再改类名与文件名。
- 若 Unity 资源或反射链路中有旧名依赖，要先保留兼容过渡方案。

### 6.3 资源/序列化敏感文件
以下文件改动时必须谨慎：
- `NodeMappingTableSO.cs`
- `EnemyDatabase.cs`
- `ItemDatabase.cs`
- `UIRootDatabase.cs`
- 任何挂载在场景或 prefab 上的管理器类

处理原则：
- 先 namespace，后重命名。
- 若需要改类名，先确认是否有 `FormerlySerializedAs` 或其他兼容手段。

## 七、分批验收标准

### 每批验收
每完成一批文件后，必须检查：
- 编译是否通过。
- 是否出现缺失 namespace、缺失 using、类型找不到、重复定义。
- Unity 关键资源是否能正常加载。
- 对应模块的核心功能是否仍可正常调用。

### 最终验收
所有批次完成后，验收标准如下：
- 解决方案编译成功。
- ReSharper 不再对大多数脚本提示 namespace 与路径不一致。
- 核心运行时模块可正常运行。
- Editor 工具可正常打开并使用。
- 关键 SO、数据库、节点映射、UI 资源未损坏。
- 历史旧名已统一或已明确保留兼容层。

## 八、建议的最终交付状态

最终希望代码库达到以下状态：
- 每个 `.cs` 文件都清楚归属某个 namespace。
- 每个模块都有稳定、可预测的命名空间层级。
- `DataDefine`、`Runtime`、`Editor` 的职责边界清晰。
- `GlobalTypeDefine.cs` 这类集中式文件已被拆分或明确保留理由。
- `EventTileManager` 这类历史命名问题已完成处理。
- 新增文件时，团队可以直接按目录生成 namespace，不再依赖猜测。
