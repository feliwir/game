using System.Collections.Generic;

namespace game.blocks
{
    public class GrassBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.GRASS;

        public GrassBlock(List<string> blockTextures)
        {
            var textures = new List<string>
            {
                "assets/textures/grass_top.png",
                "assets/textures/dirt.png",
                "assets/textures/grass_side.png",
                "assets/textures/grass_side.png",
                "assets/textures/grass_side.png",
                "assets/textures/grass_side.png"
            };
            SetTextureIDs(blockTextures, textures);
        }
    }
}
