using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

//==============================================================================//
//                                                                              //
//                                 瓦片：敌人                                    //
//                                                                              //
//==============================================================================//
[CreateAssetMenu(fileName = "EnemyTile", menuName = "Mota/EnemyTile")]
public class EnemyTile : EventTile
{
    [Tooltip("敌人ID")]
    [SerializeField] private int enemyId;

    [Tooltip("敌人名称")]
    [SerializeField] private string enemyName;

    [Tooltip("敌人描述信息")]
    [SerializeField] private string description;

    [Tooltip("敌人属性信息")]
    [SerializeField] private AttributeBonus[] _attributes; // 所有属性存在List里

    [Tooltip("敌人伤害")]
    public int Damage;

    // 重写Tile渲染逻辑：区分编辑/运行模式
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);

        // 使用运行时判断而非仅依赖预处理指令，确保在 Editor Play 与构建运行时清空 sprite
        if (!Application.isPlaying)
        {
            // 编辑模式：使用 Tile 自带的 sprite 作为占位图
            tileData.sprite = this.sprite;
            tileData.color = Color.white;
            tileData.colliderType = Tile.ColliderType.Sprite; // 保持碰撞（如果需要）
        }
        else
        {
            // 运行模式：清空贴图，避免和动画 GameObject 叠加
            tileData.sprite = null;
            tileData.color = Color.clear;
            tileData.colliderType = Tile.ColliderType.None; // 可根据需要调整
        }
    }

    private void OnEnable()
    {
        // 确保瓦片类型为敌人
        tileType = GridType.Enemy;
    }

    // 提供属性访问器
    public int EnemyId => enemyId;
    public string EnemyName => enemyName;
    public string Description => description;
    public int GetEnemyAttributeValue(AttributeType type)
    {
        // 遍历数组找到对应类型的属性值（找不到返回0）
        var attr = _attributes.FirstOrDefault(a => a.type == type);
        return attr?.value ?? 0;
    }
}