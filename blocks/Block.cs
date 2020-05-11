using game.blocks;
using System.Collections.Generic;

namespace game
{
    public abstract class Block
    {
        public static BlockType Type { get; } = BlockType.NONE;

        //those should be static
        private int TopTextureID;
        private int BottomTextureID;
        private int WestTextureID;
        private int EastTextureID;
        private int NorthTextureID;
        private int SouthTextureID;

        protected void SetTextureIDs(List<string> blockTextures, List<string> textures)
        {
            if (textures.Count == 0) return;

            while (textures.Count < 6) textures.Add(textures[0]);

            TopTextureID = SetTextureID(blockTextures, textures[0]);
            BottomTextureID = SetTextureID(blockTextures, textures[1]);
            WestTextureID = SetTextureID(blockTextures, textures[2]);
            EastTextureID = SetTextureID(blockTextures, textures[3]);
            NorthTextureID = SetTextureID(blockTextures, textures[4]);
            SouthTextureID = SetTextureID(blockTextures, textures[5]);
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
            if (textures.Contains(textureName))
            {
                return textures.IndexOf(textureName);
            }
            else
            {
                textures.Add(textureName);
                return textures.Count - 1;
            }
        }
    }
}
