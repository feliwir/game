using System.Collections.Generic;

namespace game.blocks
{
    public class OakLeavesBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.OAK_LEAVES;

        protected new List<string> Textures = new List<string> { "assets/textures/leaves_oak.png" };

        public OakLeavesBlock(List<string> blockTextures) : base(blockTextures)
        {
        }
    }
}
