using System.Collections.Generic;

namespace game.blocks
{
    public class OakLogBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.OAK_LOG;

        public OakLogBlock(List<Material> blockMaterials)
        {
            var top_bottom_material = new Material("oak_log_top.png", "oak_log_top_n.png");
            var side_material = new Material("oak_log.png", "oak_log_n.png");
            var materials = new List<Material> 
            {
                top_bottom_material,
                top_bottom_material,
                side_material
            };
            SetMaterialIDs(blockMaterials, materials);
        }
    }
}
