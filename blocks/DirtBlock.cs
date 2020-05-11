using System.Collections.Generic;

namespace game.blocks
{
    public class DirtBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.DIRT;

        public DirtBlock(List<Material> blockMaterials)
        {
            var material = new Material("dirt.png", "dirt_n.png");
            var materials = new List<Material> { material };
            SetMaterialIDs(blockMaterials, materials);
        }
    }
}
