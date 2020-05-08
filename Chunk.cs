
using game.blocks;
using lumos;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace game
{
    public class Chunk
    {
        private const int WIDTH = 16;
        private const int HEIGHT = 100;

        private Vector2 position;

        private const int STONE_HEIGHT = 3;
        private const int DIRT_HEIGHT = 1;


        private Block[,,] blocks = new Block[WIDTH, WIDTH, HEIGHT];
        private List<Block> visible_blocks = new List<Block>();

        public Chunk(Game game, Vector2 position, int seed = 0)
        {
            this.position = position;

            for (var y = 0; y < STONE_HEIGHT; y ++)
            {
                for (var x = 0; x < WIDTH; x++)
                {
                    for (var z = 0; z < WIDTH; z++)
                    {
                        if (y == 1) blocks[x, z, y] = new CoalOreBlock(new Vector3(x + position.X, y, z + position.Y));
                        else blocks[x, z, y] = new StoneBlock(new Vector3(x + position.X, y, z + position.Y));
                    }
                }
            }

            for (var y = STONE_HEIGHT; y < STONE_HEIGHT + DIRT_HEIGHT; y++)
            {
                for (var x = 0; x < WIDTH; x++)
                {
                    for (var z = 0; z < WIDTH; z++)
                    {
                        blocks[x, z, y] = new DirtBlock(new Vector3(x + position.X, y, z + position.Y));
                        blocks[x, z, y + 1] = new GrassBlock(new Vector3(x + position.X, y + 1, z + position.Y));
                    }
                }
            }

            foreach (var block in blocks)
            {
                if (block != null && block.is_visible)
                {
                    block.CreateResources(game);
                    visible_blocks.Add(block);
                }
            }
        }

        public void Update(float deltaSeconds, CommandList cl)
        {
            foreach (var block in visible_blocks)
            {
                block.Update(deltaSeconds, cl);
            }
        }

        public void Draw(CommandList cl, ResourceSet projViewSet)
        {
            foreach (var block in visible_blocks)
            {
                block.Draw(cl, projViewSet);
            }
        }
    }
}
