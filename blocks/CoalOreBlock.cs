using System.Collections.Generic;

namespace game.blocks
{
    public class CoalOreBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.COAL_ORE;

        protected new List<string> Textures = new List<string> { "assets/textures/coal_ore.png" };

        public CoalOreBlock (List<string> blockTextures) : base(blockTextures)
        {
        }
    }
}
