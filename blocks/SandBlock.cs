using System.Collections.Generic;

namespace game.blocks
{
    public class SandBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.SAND;

        protected new List<string> Textures = new List<string> { "assets/textures/sand.png" };

        public SandBlock(List<string> blockTextures) : base(blockTextures)
        {
        }
    }
}
