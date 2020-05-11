using game.blocks;
using System.Collections.Generic;

namespace game
{
    public abstract class Block
    {
        public static BlockType Type { get; } = BlockType.NONE;

        protected List<string> Textures = new List<string> { "assets/textures/default.png" };

        //those should be static
        private readonly int TopTextureID;
        private readonly int BottomTextureID;
        private readonly int WestTextureID;
        private readonly int EastTextureID;
        private readonly int NorthTextureID;
        private readonly int SouthTextureID;

        public Block (List<string> blockTextures)
        {
            if (Textures.Count == 0) return;

            while (Textures.Count < 6) Textures.Add(Textures[0]);

            TopTextureID = SetTextureID(blockTextures, Textures[0]);
            BottomTextureID = SetTextureID(blockTextures, Textures[1]);
            WestTextureID = SetTextureID(blockTextures, Textures[2]);
            EastTextureID = SetTextureID(blockTextures, Textures[3]);
            NorthTextureID = SetTextureID(blockTextures, Textures[4]);
            SouthTextureID = SetTextureID(blockTextures, Textures[5]);
        }

        // TODO: make this static
        public int GetTextureID(Direction direction)
        {
            switch(direction)
            {
                case Direction.TOP:
                    return TopTextureID;
                case Direction.BOTTOM:
                    return BottomTextureID;
                case Direction.WEST:
                    return WestTextureID;
                case Direction.EAST:
                    return EastTextureID;
                case Direction.NORTH:
                    return NorthTextureID;
                case Direction.SOUTH:
                    return SouthTextureID;
                default:
                    return 0;
            }
        }

        private int SetTextureID(List<string> textures, string textureName)
        {
            if (textures.Contains(textureName)) return textures.IndexOf(textureName);
            else
            {
                textures.Add(textureName);
                return textures.Count - 1;
            }
        }
    }
}
