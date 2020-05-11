using System.Collections.Generic;

namespace game.blocks
{
    public class SandBlock : Block
    {
        public static new BlockType Type { get; } = BlockType.SAND;

        public SandBlock(List<Material> blockMaterials)
        {
            var material = new Material("sand.png", "sand_n.png");
            var materials = new List<Material> { material };
            SetMaterialIDs(blockMaterials, materials);
        }
    }
}
