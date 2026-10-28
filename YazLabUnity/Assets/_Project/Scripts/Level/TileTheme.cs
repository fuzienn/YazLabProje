using UnityEngine;
using UnityEngine.Tilemaps;

namespace DungeonEscape.Level
{
    /// <summary>Harita kurucunun hangi hucreye hangi karoyu koyacagini tutar.</summary>
    [CreateAssetMenu(menuName = "Dungeon Escape/Tile Theme")]
    public class TileTheme : ScriptableObject
    {
        [SerializeField] private TileBase[] floorVariants;
        [SerializeField] private TileBase wallTop;
        [SerializeField] private TileBase wallFaceUpper;
        [SerializeField] private TileBase wallFaceLower;

        public TileBase WallTop => wallTop;

        public TileBase GetWall(WallShape shape) => shape switch
        {
            WallShape.FaceUpper => wallFaceUpper,
            WallShape.FaceLower => wallFaceLower,
            WallShape.Top => wallTop,
            _ => null
        };

        /// <summary>Hucre konumundan turetilen sabit desen; her kurulumda ayni gorunur.</summary>
        public TileBase GetFloor(Vector2Int cell)
        {
            int index = (cell.x & 1) + ((cell.y & 1) << 1);
            return floorVariants[index % floorVariants.Length];
        }

        public void Configure(TileBase[] floors, TileBase top, TileBase faceUpper, TileBase faceLower)
        {
            floorVariants = floors;
            wallTop = top;
            wallFaceUpper = faceUpper;
            wallFaceLower = faceLower;
        }
    }
}
