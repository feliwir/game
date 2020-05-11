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
                "assets/textures/oak_log_top.png",
                "assets/textures/oak_log_top.png",
                "assets/textures/oak_log.png",
                "assets/textures/oak_log.png",
                "assets/textures/oak_log.png",
                "assets/textures/oak_log.png"
            };
            SetTextureIDs(blockTextures, textures);
        }
    }
}
