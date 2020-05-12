using System.Collections.Generic;

namespace Viking.Blocks
{
    public class CoalOreBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.COAL_ORE;

        public CoalOreBlock(List<Material> blockMaterials)
        {
            var material = new Material("coal_ore.png", "coal_ore_n.png");
            var materials = new List<Material> { material };
            SetMaterialIDs(blockMaterials, materials);
        }
    }
}
