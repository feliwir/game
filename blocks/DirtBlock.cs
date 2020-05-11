using System.Collections.Generic;

namespace game.blocks
{
    public class DirtBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.DIRT;

        protected new List<string> Textures = new List<string> { "assets/textures/dirt.png" };

        public DirtBlock(List<string> blockTextures) : base(blockTextures)
        {
        }
    }
}
