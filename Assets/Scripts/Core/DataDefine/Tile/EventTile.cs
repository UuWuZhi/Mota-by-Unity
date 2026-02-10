using UnityEngine;
using UnityEngine.Tilemaps;
//==============================================================================//
//                                                                              //
//                                 瓦片：事件                                   //
//                                                                              //
//==============================================================================//
[CreateAssetMenu(fileName = "EventTile", menuName = "Tile/EventTile")]
public class EventTile : BaseTile
{
    [SerializeField] private bool _useStaticSprite = true;
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        base.GetTileData(position, tilemap, ref tileData);

        // 使用运行时判断而非仅依赖预处理指令，确保在 Editor Play 与构建运行时清空 sprite
        if (!Application.isPlaying || _useStaticSprite)
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
}