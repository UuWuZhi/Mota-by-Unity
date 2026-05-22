# 坐标使用规范（强制）

## 一、坐标类型与使用场景（强制）

### 1) cellPos（Vector3Int）
**定义**：格子坐标（单元格索引）。  
**使用场景**：
- 任何需要表示“格子位置”的数据、事件、注册表、路径查找、边界判断。
- 作为 Tilemap 访问入口（GetTileAtCell / SetTile / RemoveTile）。
**强制规则**：
- cellPos 必须是 Vector3Int。
- 业务层严禁用 Vector2 或 Vector3 代表格子坐标。

### 2) worldPos（Vector2）
**定义**：世界坐标（连续坐标）。  
**使用场景**：
- 输入（鼠标点击、Transform.position、Unity 物理位置）。
- 移动与动画目标位置（如 MoveToTarget）。
- 事件触发（如 PlayerArrived 事件传递）。
**强制规则**：
- 所有 world → cell 必须通过 GridManager.TryWorldToCellPos。
- 不允许自行 WorldToCell 或 Mathf.Floor 等替代方案（除非 GridManager 提供接口）。

## 二、格子中心与格子角落（命名与语义强制）

### 1) cellCenterWorldPos（Vector2）
**定义**：格子中心的世界坐标。  
**使用场景**：
- 仅用于可视化/渲染（例如路径线绘制、Gizmos）。
- 需要“居中显示”的 UI/动画/调试。
**强制规则**：
- 命名必须为 cellCenterWorldPos。
- 获取方式必须为 GridManager.GetCellCenterWorld(cellPos)。

### 2) cellOriginWorldPos（Vector2）
**定义**：格子左下角的世界坐标（Cell 原点）。  
**使用场景**：
- 玩家移动目标、坐标对齐到整数（当前系统约定）。
- 出生点、物理坐标对齐。
**强制规则**：
- 命名必须为 cellOriginWorldPos 或 cellOriginWorld（含 World 字样）。
- 获取方式必须为 GridManager.GetCellOriginWorld(cellPos)。

## 三、统一坐标转换规范（强制）

- World → Cell：必须使用 GridManager.TryWorldToCellPos(worldPos, out cellPos)。
- Cell → World（中心）：必须使用 GridManager.GetCellCenterWorld(cellPos)。
- Cell → World（角落）：必须使用 GridManager.GetCellOriginWorld(cellPos)。
- 禁止任何模块直接使用 mapGrid.WorldToCell / mapGrid.CellToWorld。

## 四、坐标使用整理路径（模块级链路）

1. GridManager（坐标核心）
   - 唯一合法的坐标转换入口。
   - 统一提供 TryWorldToCellPos / GetCellCenterWorld / GetCellOriginWorld。

2. MapManager / MapLayerInfo（地图与出生点）
   - 出生点 SpawnPoint.position 属于 worldPos。
   - 若需要格子逻辑，必须经 TryWorldToCellPos 转换后再处理。

3. PlayerMovement（移动与路径）
   - 路径 / 判断：使用 cellPos。
   - 移动目标：使用 cellOriginWorldPos。
   - 绘制路径线：使用 cellCenterWorldPos（仅可视化）。

4. EventTileManager / EventTileRegistry（事件瓦片）
   - 注册 / 查找：必须使用 cellPos。
   - 触发来源若为 worldPos，必须先 TryWorldToCellPos。
