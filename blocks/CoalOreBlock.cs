using System.Collections.Generic;

namespace game.blocks
{
    public class CoalOreBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.COAL_ORE;

        public CoalOreBlock (List<string> blockTextures)
        {
            var textures = new List<string> { "assets/textures/coal_ore.png" };
            SetTextureIDs(blockTextures, textures);
        }
    }
}
