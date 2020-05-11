using System.Collections.Generic;

namespace game.blocks
{
    public class StoneBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.STONE;

        protected new List<string> Textures = new List<string> { "assets/textures/stone.png" };

        public StoneBlock(List<string> blockTextures) : base(blockTextures)
        {
        }
    }
}
