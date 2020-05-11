using System.Collections.Generic;

namespace game.blocks
{
    public class DirtBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.DIRT;

        public DirtBlock(List<string> blockTextures)
        {
            var textures = new List<string> { "assets/textures/dirt.png" };
            SetTextureIDs(blockTextures, textures);
        }
    }
}
