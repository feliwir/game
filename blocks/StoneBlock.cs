using System.Collections.Generic;

namespace Viking.Blocks
{
    public class StoneBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.STONE;

        public StoneBlock(List<Material> blockMaterials)
        {
            var material = new Material("stone.png", "stone_n.png");
            var materials = new List<Material> { material };
            SetMaterialIDs(blockMaterials, materials);
        }
    }
}
