using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;
using Viking.Blocks;

namespace Viking.Map
{
    public class Chunk
    {
        public int X { get; private set; }
        public int Z { get; private set; }

        public const int WIDTH = 16;
        public const int HEIGHT = 100;

        public BlockType[,,] Blocks = new BlockType[WIDTH, HEIGHT, WIDTH];
        internal MeshGenerator Generator { get; }
        private const int STONE_HEIGHT = 5;
        private const int DIRT_HEIGHT = 16;
        private static Pipeline Pipeline;
        protected static ResourceSet WorldTextureSet;

        private Vector3 m_world;
        private static DeviceBuffer WorldBuffer;
        private DeviceBuffer m_indexBuffer;
        private DeviceBuffer m_vertexBuffer;
        private bool m_dirty = true;

        public Chunk(int x, int y, int[,] heightMap, Random random)
        {
            X = x;
            Z = y;
            Generator = new MeshGenerator();
            m_world = new Vector3(x, 0, y);

            Generate(heightMap, random);
        }

        private void Generate(int[,] heightMap, Random random)
        {
            //create bottom stone blocks
            for (var y = 0; y < STONE_HEIGHT; y++)
            {
                for (var x = 0; x < WIDTH; x++)
                {
                    for (var z = 0; z < WIDTH; z++)
                    {
                        Blocks[x, y, z] = BlockType.STONE;
                    }
                }
            }

            //create dirt and grass
            for (var x = 0; x < WIDTH; x++)
            {
                for (var z = 0; z < WIDTH; z++)
                {
                    int y;
                    var height = heightMap[x + X, z + Z];
                    for (y = STONE_HEIGHT; y < STONE_HEIGHT + DIRT_HEIGHT + height; y++)
                    {
                        Blocks[x, y, z] = BlockType.DIRT;
                    }
                    Blocks[x, y, z] = BlockType.GRASS;
                }
            }

            //Tree.Generate(blocks, 8, 4, 8, random);
        }

        public static void CreateResources(Game game)
        {
            WorldBuffer = game.Factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            var vertexShaderCode = System.IO.File.ReadAllText("shaders/chunk.vert");
            var fragmentShaderCode = System.IO.File.ReadAllText("shaders/chunk.frag");

            var vertexLayout = new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                        new VertexElementDescription("MaterialID", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1),
                        new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                        new VertexElementDescription("FaceDirection", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1));

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new VertexLayoutDescription[] { vertexLayout },
                game.Factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexShaderCode), "main"),
                    new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentShaderCode), "main")));

            ResourceLayout projViewLayout = game.Factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            ResourceLayout worldTextureLayout = game.Factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("DiffuseTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("NormalMap", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            Pipeline = game.Factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { projViewLayout, worldTextureLayout },
                game.Swapchain.Framebuffer.OutputDescription));

            WorldTextureSet = game.Factory.CreateResourceSet(new ResourceSetDescription(
                worldTextureLayout,
                WorldBuffer,
                game.BlockDiffuseTextureArray,
                game.BlockNormalMapArray,
                game.GraphicsDevice.Aniso4xSampler));
        }

        public void Draw(CommandList cl, ResourceSet projViewSet)
        {
            cl.UpdateBuffer(WorldBuffer, 0, m_world);

            cl.SetPipeline(Pipeline);
            cl.SetVertexBuffer(0, m_vertexBuffer);
            cl.SetIndexBuffer(m_indexBuffer, IndexFormat.UInt16);
            cl.SetGraphicsResourceSet(0, projViewSet);
            cl.SetGraphicsResourceSet(1, WorldTextureSet);
            cl.DrawIndexed((uint)Generator.Indices.Count, 1, 0, 0, 0);
        }

        private void CreateVertices(Game game)
        {
            GreedyMesh.ReduceMesh(this, game);

            m_vertexBuffer = game.Factory.CreateBuffer(new BufferDescription((uint)(VertexType.SizeInBytes * Generator.Vertices.Count), BufferUsage.VertexBuffer));
            game.GraphicsDevice.UpdateBuffer(m_vertexBuffer, 0, Generator.Vertices.ToArray());

            m_indexBuffer = game.Factory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)Generator.Indices.Count, BufferUsage.IndexBuffer));
            game.GraphicsDevice.UpdateBuffer(m_indexBuffer, 0, Generator.Indices.ToArray());
        }

        internal BlockType GetBlockAt(int x, int y, int z, Game game)
        {
            var southChunkPosition = new Tuple<int, int>(X, Z + 1);
            var northChunkPosition = new Tuple<int, int>(X, Z - 1);
            var eastChunkPosition = new Tuple<int, int>(X + 1, Z);
            var westChunkPosition = new Tuple<int, int>(X - 1, Z);

            if (y < 0 || y >= HEIGHT) return BlockType.NONE;

            if (x < 0)
            {
                return game.GetBlockAt(westChunkPosition, x + WIDTH, y, z);
            }
            else if (x >= WIDTH)
            {
                return game.GetBlockAt(eastChunkPosition, x - WIDTH, y, z);
            }

            if (z < 0)
            {
                return game.GetBlockAt(northChunkPosition, x, y, z + WIDTH);
            }
            else if (z >= WIDTH)
            {
                return game.GetBlockAt(southChunkPosition, x, y, z - WIDTH);
            }

            return Blocks[x, y, z];
        }

        public void Update(float deltaSeconds, Game game)
        {
            if (m_dirty)
            {
                CreateVertices(game);
                m_dirty = false;
            }
        }

    }
}
