using UnityEngine;

namespace DungeonEscape.Characters
{
    public enum Facing
    {
        Down,
        Up,
        Side
    }

    /// <summary>Bir karakterin yon bazli kareleri: [0] durus, [1] ve [2] yurume.</summary>
    [CreateAssetMenu(menuName = "Dungeon Escape/Character Sprite Set")]
    public class CharacterSpriteSet : ScriptableObject
    {
        [SerializeField] private Sprite[] down = new Sprite[3];
        [SerializeField] private Sprite[] up = new Sprite[3];
        [SerializeField] private Sprite[] side = new Sprite[3];

        public Sprite[] Get(Facing facing) => facing switch
        {
            Facing.Up => up,
            Facing.Side => side,
            _ => down
        };

        public void Configure(Sprite[] downFrames, Sprite[] upFrames, Sprite[] sideFrames)
        {
            down = downFrames;
            up = upFrames;
            side = sideFrames;
        }
    }
}
