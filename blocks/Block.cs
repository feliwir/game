using System.Collections.Generic;
using System.Linq;

namespace Viking.Blocks
{
    public abstract class Block
    {
        public static BlockType Type { get; } = BlockType.NONE;

        //those should be static
        private int TopMaterialID;
        private int BottomMaterialID;
        private int WestMaterialID;
        private int EastMaterialID;
        private int NorthMaterialID;
        private int SouthMaterialID;

        protected void SetMaterialIDs(List<Material> blockMaterials, List<Material> materials)
        {
            if (materials.Count == 0) return;

            while (materials.Count < 6) materials.Add(materials.Last());

            TopMaterialID = SetMaterialID(blockMaterials, materials[0]);
            BottomMaterialID = SetMaterialID(blockMaterials, materials[1]);
            WestMaterialID = SetMaterialID(blockMaterials, materials[2]);
            EastMaterialID = SetMaterialID(blockMaterials, materials[3]);
            NorthMaterialID = SetMaterialID(blockMaterials, materials[4]);
            SouthMaterialID = SetMaterialID(blockMaterials, materials[5]);
        }

        // TODO: make this static
        public int GetMaterialID(Direction direction)
        {
            switch (direction)
            {
                case Direction.UP:
                    return TopMaterialID;
                case Direction.DOWN:
                    return BottomMaterialID;
                case Direction.WEST:
                    return WestMaterialID;
                case Direction.EAST:
                    return EastMaterialID;
                case Direction.NORTH:
                    return NorthMaterialID;
                case Direction.SOUTH:
                    return SouthMaterialID;
                default:
                    return 0;
            }
        }

        private int SetMaterialID(List<Material> materials, Material material)
        {
            if (materials.Contains(material))
            {
                return materials.IndexOf(material);
            }
            else
            {
                materials.Add(material);
                return materials.Count - 1;
            }
        }
    }
}
