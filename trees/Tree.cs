
using game.blocks;
using System;

namespace game.trees
{
    public abstract class Tree
    {
        public static void Generate(BlockType[,,] blocks, int x, int y, int z, Random random)
        {
            var height = random.Next(3, 5);
            for (var i = 0; i < height; i++)
            {
                blocks[x, y + i, z] = BlockType.OAK_LOG;
            }
            
            blocks[x, y + height + 0, z] = BlockType.OAK_LEAVES;
            blocks[x, y + height + 1, z] = BlockType.OAK_LEAVES;
            blocks[x, y + height + 2, z] = BlockType.OAK_LEAVES;
            blocks[x, y + height + 3, z] = BlockType.OAK_LEAVES;

            blocks[x - 1, y + height + 0, z] = BlockType.OAK_LEAVES;
            blocks[x - 1, y + height + 1, z] = BlockType.OAK_LEAVES;
            blocks[x - 1, y + height + 2, z] = BlockType.OAK_LEAVES;
            blocks[x - 1, y + height + 3, z] = BlockType.OAK_LEAVES;

            blocks[x + 1, y + height + 0, z] = BlockType.OAK_LEAVES;
            blocks[x + 1, y + height + 1, z] = BlockType.OAK_LEAVES;
            blocks[x + 1, y + height + 2, z] = BlockType.OAK_LEAVES;
            blocks[x + 1, y + height + 3, z] = BlockType.OAK_LEAVES;

            blocks[x, y + height + 0, z - 1] = BlockType.OAK_LEAVES;
            blocks[x, y + height + 1, z - 1] = BlockType.OAK_LEAVES;
            blocks[x, y + height + 2, z - 1] = BlockType.OAK_LEAVES;
            blocks[x, y + height + 3, z - 1] = BlockType.OAK_LEAVES;

            blocks[x, y + height + 0, z + 1] = BlockType.OAK_LEAVES;
            blocks[x, y + height + 1, z + 1] = BlockType.OAK_LEAVES;
            blocks[x, y + height + 2, z + 1] = BlockType.OAK_LEAVES;
            blocks[x, y + height + 3, z + 1] = BlockType.OAK_LEAVES;
        }
    }
}
