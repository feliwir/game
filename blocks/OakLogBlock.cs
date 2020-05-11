using System.Collections.Generic;

namespace game.blocks
{
    public class OakLogBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.OAK_LOG;

        public OakLogBlock(List<string> blockTextures)
        {
            var textures = new List<string>
            {
                "assets/textures/log_oak_top.png",
                "assets/textures/log_oak_top.png",
                "assets/textures/log_oak.png",
                "assets/textures/log_oak.png",
                "assets/textures/log_oak.png",
                "assets/textures/log_oak.png"
            };
            SetTextureIDs(blockTextures, textures);
        }
    }
}
