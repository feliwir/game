using System.Collections.Generic;

namespace game.blocks
{
    public class SandBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.SAND;

        public SandBlock(List<string> blockTextures)
        {
            var textures = new List<string> { "assets/textures/sand.png" };
            SetTextureIDs(blockTextures, textures);
        }
    }
}
