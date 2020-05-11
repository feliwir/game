using System.Collections.Generic;

namespace game.blocks
{
    public class GrassBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.GRASS;

        public GrassBlock(List<Material> blockMaterials)
        {
            var topMaterial = new Material("grass_block_top.png", "grass_block_top_n.png");
            var sideMaterial = new Material("grass_block_side.png", "grass_block_side_n.png");
            var bottomMaterial = new Material("dirt.png", "dirt_n.png");

            var materials = new List<Material>
            {
                topMaterial,
                bottomMaterial,
                sideMaterial,
                sideMaterial,
                sideMaterial,
                sideMaterial
            };
            SetMaterialIDs(blockMaterials, materials);
        }
    }
}
