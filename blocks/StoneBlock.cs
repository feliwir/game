using System.Collections.Generic;

namespace game.blocks
{
    public class StoneBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.STONE;

        public StoneBlock(List<string> blockTextures)
        {
            var textures = new List<string> { "assets/textures/stone.png" };
            SetTextureIDs(blockTextures, textures);
        }
    }
}
