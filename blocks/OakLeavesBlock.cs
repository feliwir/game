using System.Collections.Generic;

namespace game.blocks
{
    public class OakLeavesBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.OAK_LEAVES;

        public OakLeavesBlock(List<string> blockTextures)
        {
            var textures = new List<string> { "assets/textures/leaves_oak.png" };
            SetTextureIDs(blockTextures, textures);
        }
    }
}
