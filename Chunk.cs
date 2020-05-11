using game.blocks;
using lumos;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace game
{
    public class Chunk
    {
        public int X { get; private set; }
        public int Z { get; private set; }

        public const int WIDTH = 16;
        private const int HEIGHT = 100;

        public BlockType[,,] Blocks = new BlockType[WIDTH, HEIGHT, WIDTH];

        private const int STONE_HEIGHT = 5;
        private const int DIRT_HEIGHT = 16;

        private static Pipeline m_pipeline;
        protected static ResourceSet worldTextureSet;

        private Vector3 m_world;
        private static DeviceBuffer m_worldBuffer;
        private DeviceBuffer indexBuffer;
        private List<ushort> indices = new List<ushort>();
        private DeviceBuffer vertexBuffer;
        private List<VertexType> vertices = new List<VertexType>();

        private bool dirty = true;

        public Chunk(int x, int y, int[,] heightMap, Random random)
        {
            X = x;
            Z = y;
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
                    var height = heightMap[x + (int)m_world.X, z + (int)m_world.Z];
                    for (var y = STONE_HEIGHT; y < STONE_HEIGHT + DIRT_HEIGHT + height; y++)
                    {
                        Blocks[x, y, z] = BlockType.DIRT;
                        Blocks[x, y + 1, z] = BlockType.GRASS;
                    }
                }
            }

            //Tree.Generate(blocks, 8, 4, 8, random);
        }

        public static void CreateResources(Game game)
        {
            m_worldBuffer = game.Factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

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

            m_pipeline = game.Factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { projViewLayout, worldTextureLayout },
                game.Swapchain.Framebuffer.OutputDescription));

            worldTextureSet = game.Factory.CreateResourceSet(new ResourceSetDescription(
                worldTextureLayout,
                m_worldBuffer,
                game.BlockDiffuseTextureArray,
                game.BlockNormalMapArray,
                game.GraphicsDevice.Aniso4xSampler));
        }

        public void Draw(CommandList cl, ResourceSet projViewSet)
        {
            cl.UpdateBuffer(m_worldBuffer, 0, m_world);

            cl.SetPipeline(m_pipeline);
            cl.SetVertexBuffer(0, vertexBuffer);
            cl.SetIndexBuffer(indexBuffer, IndexFormat.UInt16);
            cl.SetGraphicsResourceSet(0, projViewSet);
            cl.SetGraphicsResourceSet(1, worldTextureSet);
            cl.DrawIndexed((uint)indices.Count, 1, 0, 0, 0);
        }

        private void CreateVertices(Game game)
        {
            for (var y = 0; y < HEIGHT; y++)
            {
                for (var x = 0; x < WIDTH; x++)
                {
                    for (var z = 0; z < WIDTH; z++)
                    {
                        var blockType = Blocks[x, y, z];
                        if (blockType == BlockType.NONE) continue;

                        var block = game.BlockTypes[blockType];

                        var topBlock = y < HEIGHT - 1 ? Blocks[x, y + 1, z] : BlockType.NONE;
                        if ((int)topBlock < 1) AddFace(x, y, z, block.GetMaterialID(Direction.TOP), Direction.TOP);

                        var bottomBlock = y > 0 ? Blocks[x, y - 1, z] : BlockType.NONE;
                        if ((int)bottomBlock < 1) AddFace(x, y, z, block.GetMaterialID(Direction.BOTTOM), Direction.BOTTOM);

                        var westBlock = x > 0 ? Blocks[x - 1, y, z] : game.GetBlockAt(X - 1, y, z);
                        if ((int)westBlock < 1) AddFace(x, y, z, block.GetMaterialID(Direction.WEST), Direction.WEST);

                        var eastBlock = x < WIDTH - 1 ? Blocks[x + 1, y, z] : game.GetBlockAt(X + WIDTH, y, z);
                        if ((int)eastBlock < 1) AddFace(x, y, z, block.GetMaterialID(Direction.EAST), Direction.EAST);

                        var northBlock = z < WIDTH - 1 ? Blocks[x, y, z + 1] : game.GetBlockAt(x, y, Z + WIDTH);
                        if ((int)northBlock < 1) AddFace(x, y, z, block.GetMaterialID(Direction.NORTH), Direction.NORTH);

                        var southBlock = z > 0 ? Blocks[x, y, z - 1] : game.GetBlockAt(x, y, Z - 1);
                        if ((int)southBlock < 1) AddFace(x, y, z, block.GetMaterialID(Direction.SOUTH), Direction.SOUTH);
                    }
                }
            }

            vertexBuffer = game.Factory.CreateBuffer(new BufferDescription((uint)(VertexType.SizeInBytes * vertices.Count), BufferUsage.VertexBuffer));
            game.GraphicsDevice.UpdateBuffer(vertexBuffer, 0, vertices.ToArray());

            indexBuffer = game.Factory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)indices.Count, BufferUsage.IndexBuffer));
            game.GraphicsDevice.UpdateBuffer(indexBuffer, 0, indices.ToArray());
        }

        public void Update(float deltaSeconds, Game game)
        {
            if (dirty)
            {
                CreateVertices(game);
                dirty = false;
            }
        }

        private void AddFace(int x, int y, int z, int texId, Direction direction)
        {
            var verts = south_vertices;
            var offset = new Vector3(x, y, z);
            switch (direction)
            {
                case Direction.TOP:
                    verts = top_vertices;
                    break;
                case Direction.BOTTOM:
                    verts = bottom_vertices;
                    break;
                case Direction.WEST:
                    verts = west_vertices;
                    break;
                case Direction.EAST:
                    verts = east_vertices;
                    break;
                case Direction.NORTH:
                    verts = north_vertices;
                    break;
                case Direction.SOUTH:
                    verts = south_vertices;
                    break;
            }

            var index = (ushort)vertices.Count;
            indices.Add(index);
            indices.Add((ushort)(index + 1));
            indices.Add((ushort)(index + 2));

            indices.Add(index);
            indices.Add((ushort)(index + 2));
            indices.Add((ushort)(index + 3));

            for (var i = 0; i < 4; i++)
            {
                vertices.Add(new VertexType(verts[i] + offset, texId, uv_coords[i], direction));
            }
        }

        private static List<Vector3> top_vertices = new List<Vector3>
        { 
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 1f, 1f)
        };

        private static List<Vector3> bottom_vertices = new List<Vector3>
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(1f, 0f, 1f)
        };

        private static List<Vector3> west_vertices = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 1f, 1f),
            new Vector3(0f, 0f, 1f)
        };

        private static List<Vector3> east_vertices = new List<Vector3>
        {
            new Vector3(1f, 0f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 0f, 0f)
        };

        private static List<Vector3> north_vertices = new List<Vector3>
        {
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 1f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(1f, 0f, 1f)
        };

        private static List<Vector3> south_vertices = new List<Vector3>
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0f, 0f)
        };

        private static List<Vector2> uv_coords = new List<Vector2>
        {
            // using 1.0 causes the grass_side texture to cause a small green stripe
            new Vector2(0, 0.99f),
            new Vector2(0, 0),
            new Vector2(0.99f, 0),
            new Vector2(0.99f, 0.99f),
            
        };

        [StructLayout(LayoutKind.Sequential)]
        private struct VertexType
        {
            public const uint SizeInBytes = 28;

            public Vector3 Position;
            public int MaterialID;
            public Vector2 TexCoords;
            public int FaceDirection;

            public VertexType(Vector3 pos, int matId, Vector2 uv, Direction faceDir)
            {
                Position = pos;
                MaterialID = matId;
                TexCoords = uv;
                FaceDirection = (int)faceDir;
            }
        }
    }
}
