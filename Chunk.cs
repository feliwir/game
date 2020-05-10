using game.blocks;
using lumos;
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

        private int[,,] blocks = new int[WIDTH, WIDTH, HEIGHT];

        private const int STONE_HEIGHT = 2;
        private const int DIRT_HEIGHT = 1;

        private Pipeline m_pipeline;
        protected ResourceSet worldTextureSet;

        private Vector3 m_world;
        private DeviceBuffer m_worldBuffer;
        private DeviceBuffer indexBuffer;
        private List<ushort> indices = new List<ushort>();
        private DeviceBuffer vertexBuffer;
        private List<VertexPositionTexture> vertices = new List<VertexPositionTexture>();

        public Chunk(Game game, Vector3 position, int seed = 0)
        {
            m_world = position;
            Generate();
            CreateResources(game);
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
                        blocks[x, z, y] = 1; // TODO: Use a block type here
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
                        blocks[x, z, y] = 2; // TODO: Use a block type here
                    }
                }
            }

            //create grass
            for (var x = 0; x < WIDTH; x++)
            {
                for (var z = 0; z < WIDTH; z++)
                {
                    blocks[x, z, STONE_HEIGHT + DIRT_HEIGHT] = 3; // TODO: Use a block type here
                }
            }
        }

        private void CreateResources(Game game)
        {
            m_worldBuffer = game.Factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            CreateVertices(game);

            var texData = new ImageSharpTexture("assets/stone.png");
            var surfaceTexture = texData.CreateDeviceTexture(game.GraphicsDevice, game.Factory);

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
                surfaceTexture,
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
                        var blockType = blocks[x, z, y];
                        if (blockType == 0) continue;

                        //TODO: handle neighboring chunks

                        //var topBlock = y < HEIGHT ? blocks[x, z, y + 1] : 0;
                        //if (topBlock == 0) 
                        AddFace(x, z, y, blockType, FaceType.Top);

                        //var bottomBlock = y > 0 ? blocks[x, z, y - 1] : 0;
                        //if (bottomBlock == 0)
                        AddFace(x, z, y, blockType, FaceType.Bottom);

                        //var leftBlock = x > 0 ? blocks[x - 1, z, y] : 0;
                        //if (leftBlock == 0)
                        AddFace(x, z, y, blockType, FaceType.Left);

                        //var rightBlock = x < WIDTH ? blocks[x + 1, z, y] : 0;
                        //if (rightBlock == 0)
                        AddFace(x, z, y, blockType, FaceType.Right);

                        //var backBlock = z < WIDTH ? blocks[x, z + 1, y] : 0;
                        //if (backBlock == 0)
                        AddFace(x, z, y, blockType, FaceType.Back);

                        //var frontBlock = z > 0 ? blocks[x, z - 1, y] : 0;
                        //if (frontBlock == 0)
                        AddFace(x, z, y, blockType, FaceType.Front);
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

        private struct InstanceInfo
        {
            public const uint SizeInBytes = 12;

            public Vector3 Position;

            public InstanceInfo(Vector3 position)
            {
                Position = position;
            }
        }

        private void AddFace(int x, int z, int y, int texId, FaceType face_type)
        {
            var verts = front_vertices;
            var offset = new Vector3(x, z, y);
            switch (face_type)
            {
                case FaceType.Top:
                    verts = top_vertices;
                    break;
                case FaceType.Bottom:
                    verts = bottom_vertices;
                    break;
                case FaceType.Left:
                    verts = left_vertices;
                    break;
                case FaceType.Right:
                    verts = right_vertices;
                    break;
                case FaceType.Back:
                    verts = back_vertices;
                    break;
                case FaceType.Front:
                    verts = front_vertices;
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

        private enum FaceType
        {
            Top,
            Bottom,
            Left,
            Right,
            Back,
            Front
        };

        private List<Vector3> top_vertices = new List<Vector3>
        { 
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 1f, 1f)
        };

        private List<Vector3> bottom_vertices = new List<Vector3>
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(1f, 0f, 1f)
        };

        private List<Vector3> left_vertices = new List<Vector3>
        {
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 1f, 1f)
        };

        private List<Vector3> right_vertices = new List<Vector3>
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 0f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(1f, 1f, 0f)
        };

        private List<Vector3> back_vertices = new List<Vector3>
        {
            new Vector3(1f, 0f, 1f),
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 1f, 1f),
            new Vector3(1f, 1f, 1f)
        };

        private List<Vector3> front_vertices = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 1f, 0f)
        };

        private List<Vector2> uv_coords = new List<Vector2>
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
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
