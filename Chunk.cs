
using game.blocks;
using lumos;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace game
{
    public class Chunk
    {
        private const int WIDTH = 10;
        private const int HEIGHT = 100;

        private Vector2 position;

        private const int STONE_HEIGHT = 2;
        private const int DIRT_HEIGHT = 1;

        const int INSTANCE_COUNT = 200;
        public DeviceBuffer _instanceBuffer;
        private InstanceInfo[] infos = new InstanceInfo[INSTANCE_COUNT];
        private StoneBlock block = new StoneBlock(new Vector3());

        public Chunk(Game game, Vector2 position, int seed = 0)
        {
            this.position = position;

            CreateResources(game);
            block.CreateResources(game);

            var i = 0;
            for (var y = 0; y < STONE_HEIGHT; y++)
            {
                for (var x = 0; x < WIDTH; x++)
                {
                    for (var z = 0; z < WIDTH; z++)
                    {
                        infos[i++] = new InstanceInfo { Position = new Vector3(x + position.X, y, z + position.Y) };
                        //if (y == 1) blocks[x, z, y] = new CoalOreBlock(new Vector3(x + position.X, y, z + position.Y));
                        //else blocks[x, z, y] = new StoneBlock(new Vector3(x + position.X, y, z + position.Y));
                    }
                }
            }

            game.GraphicsDevice.UpdateBuffer(_instanceBuffer, 0, infos);

            //for (var y = STONE_HEIGHT; y < STONE_HEIGHT + DIRT_HEIGHT; y++)
            //{
            //    for (var x = 0; x < WIDTH; x++)
            //    {
            //        for (var z = 0; z < WIDTH; z++)
            //        {
            //            blocks[x, z, y] = new DirtBlock(new Vector3(x + position.X, y, z + position.Y));
            //            blocks[x, z, y + 1] = new GrassBlock(new Vector3(x + position.X, y + 1, z + position.Y));
            //        }
            //    }
            //}

            //foreach (var block in blocks)
            //{
            //    if (block != null && block.is_visible)
            //    {
            //        block.CreateResources(game);
            //        visible_blocks.Add(block);
            //    }
            //}
        }

        private void CreateResources(Game game)
        {
            _instanceBuffer = game.Factory.CreateBuffer(new BufferDescription(InstanceInfo.SizeInBytes * INSTANCE_COUNT, BufferUsage.VertexBuffer));
        }

        public void Update(float deltaSeconds, CommandList cl)
        {
            //cl.UpdateBuffer(_instanceBuffer, 0, infos);
            block.Update(deltaSeconds, cl);
        }

        public void Draw(CommandList cl, ResourceSet projViewSet)
        {
            block.Draw(cl, projViewSet, _instanceBuffer);
        }

        private struct InstanceInfo
        {
            public const uint SizeInBytes = 12;

            public Vector3 Position;

            public InstanceInfo(Vector3 position)
            {
                Position = position;
            }
        }
    }
}
