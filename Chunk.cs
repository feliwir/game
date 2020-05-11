using game.blocks;
using lumos;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;

namespace game
{
    public class Chunk
    {
        private const int WIDTH = 16;
        private const int HEIGHT = 100;

        private BlockType[,,] blocks = new BlockType[WIDTH, HEIGHT, WIDTH];

        private const int STONE_HEIGHT = 2;
        private const int DIRT_HEIGHT = 1;

        private static Pipeline m_pipeline;
        protected static ResourceSet worldTextureSet;

        private Vector3 m_world;
        private static DeviceBuffer m_worldBuffer;
        private DeviceBuffer indexBuffer;
        private List<ushort> indices = new List<ushort>();
        private DeviceBuffer vertexBuffer;
        private List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();

        public Chunk(Game game, Vector3 position, int seed = 0)
        {
            m_world = position;
            Generate();
            CreateVertices(game);
        }

        private void Generate()
        {
            //create bottom stone blocks
            for (var y = 0; y < STONE_HEIGHT; y++)
            {
                for (var x = 0; x < WIDTH; x++)
                {
                    for (var z = 0; z < WIDTH; z++)
                    {
                        blocks[x, y, z] = BlockType.STONE;
                    }
                }
            }

            //create dirt
            for (var y = STONE_HEIGHT; y < STONE_HEIGHT + DIRT_HEIGHT; y++)
            {
                for (var x = 0; x < WIDTH; x++)
                {
                    for (var z = 0; z < WIDTH; z++)
                    {
                        blocks[x, y, z] = BlockType.DIRT;
                    }
                }
            }

            //create grass
            for (var x = 0; x < WIDTH; x++)
            {
                for (var z = 0; z < WIDTH; z++)
                {
                    blocks[x, STONE_HEIGHT + DIRT_HEIGHT, z] = BlockType.GRASS;
                }
            }
        }

        public static void CreateResources(Game game)
        {
            m_worldBuffer = game.Factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            var vertexShaderCode = System.IO.File.ReadAllText("shaders/chunk.vert");
            var fragmentShaderCode = System.IO.File.ReadAllText("shaders/chunk.frag");

            var vertexLayout = new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                        new VertexElementDescription("TexID", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1),
                        new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));

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
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
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
                game.BlockTextureArray,
                game.GraphicsDevice.Aniso4xSampler));
        }

        public void Draw(CommandList cl, ResourceSet projViewSet)
        {
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
                        var blockType = blocks[x, y, z];
                        if (blockType == 0) continue;

                        //TODO: handle neighboring chunks
                        var block = game.BlockTypes[blockType];

                        var topBlock = y < HEIGHT - 1 ? blocks[x, y + 1, z] : 0;
                        if (topBlock == 0) AddFace(x, y, z, block.GetTextureID(Direction.TOP), Direction.TOP);

                        var bottomBlock = y > 0 ? blocks[x, y - 1, z] : 0;
                        if (bottomBlock == 0) AddFace(x, y, z, block.GetTextureID(Direction.BOTTOM), Direction.BOTTOM);

                        var leftBlock = x > 0 ? blocks[x - 1, y, z] : 0;
                        if (leftBlock == 0) AddFace(x, y, z, block.GetTextureID(Direction.WEST), Direction.WEST);

                        var rightBlock = x < WIDTH - 1 ? blocks[x + 1, y, z] : 0;
                        if (rightBlock == 0) AddFace(x, y, z, block.GetTextureID(Direction.EAST), Direction.EAST);

                        var backBlock = z < WIDTH - 1 ? blocks[x, y, z + 1] : 0;
                        if (backBlock == 0) AddFace(x, y, z, block.GetTextureID(Direction.NORTH), Direction.NORTH);

                        var frontBlock = z > 0 ? blocks[x, y, z - 1] : 0;
                        if (frontBlock == 0) AddFace(x, y, z, block.GetTextureID(Direction.SOUTH), Direction.SOUTH);
                    }
                }
            }

            vertexBuffer = game.Factory.CreateBuffer(new BufferDescription((uint)(VertexPositionTexture.SizeInBytes * vertices.Count), BufferUsage.VertexBuffer));
            game.GraphicsDevice.UpdateBuffer(vertexBuffer, 0, vertices.ToArray());

            indexBuffer = game.Factory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)indices.Count, BufferUsage.IndexBuffer));
            game.GraphicsDevice.UpdateBuffer(indexBuffer, 0, indices.ToArray());
        }

        public void Update(float deltaSeconds, CommandList cl)
        {
           
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
                vertices.Add(new VertexPositionTexture(verts[i] + offset, texId, uv_coords[i]));
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
            new Vector2(0, 1),
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            
        };

        private struct VertexPositionTexture
        {
            public const uint SizeInBytes = 24;

            public float PosX;
            public float PosY;
            public float PosZ;

            public int TexID;

            public float TexU;
            public float TexV;

            public VertexPositionTexture(Vector3 pos, int texId, Vector2 uv)
            {
                PosX = pos.X;
                PosY = pos.Y;
                PosZ = pos.Z;
                TexID = texId;
                TexU = uv.X;
                TexV = uv.Y;
            }
        }
    }
}
