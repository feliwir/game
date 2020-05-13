using System.Collections.Generic;

namespace Viking.Blocks
{
    public class OakLeavesBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.OAK_LEAVES;

        public OakLeavesBlock(List<Material> blockMaterials)
        {
            var material = new Material("oak_leaves.png");
            var materials = new List<Material> { material };
            SetMaterialIDs(blockMaterials, materials);
        }
    }
}
