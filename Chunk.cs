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

        private int[,,] blocks = new int[WIDTH, HEIGHT, WIDTH];

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
                        blocks[x, y, z] = 1; // TODO: Use a block type here
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
                        blocks[x, y, z] = 2; // TODO: Use a block type here
                    }
                }
            }

            //create grass
            for (var x = 0; x < WIDTH; x++)
            {
                for (var z = 0; z < WIDTH; z++)
                {
                    blocks[x, STONE_HEIGHT + DIRT_HEIGHT, z] = 3; // TODO: Use a block type here
                }
            }
        }

        private void CreateResources(Game game)
        {
            m_worldBuffer = game.Factory.CreateBuffer(new BufferDescription(16, BufferUsage.UniformBuffer));

            CreateVertices(game);

            var textureNames = new List<string> { "assets/stone.png", "assets/dirt.png", "assets/grass_top.png" };

            var textureArray = CreateTextureArray(textureNames, game);

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
                textureArray,
                game.GraphicsDevice.Aniso4xSampler));
        }

        private Texture CreateTextureArray(List<string> textureNames, Game game)
        {
            var largestTextureSize = uint.MinValue;
            var textures = new List<ImageSharpTexture>();

            foreach (var textureName in textureNames)
            {
                var texture = new ImageSharpTexture(textureName);
                textures.Add(texture);
                largestTextureSize = Math.Max(largestTextureSize, texture.Width);
            }

            var textureArray = game.GraphicsDevice.ResourceFactory.CreateTexture(
                TextureDescription.Texture2D(
                    largestTextureSize,
                    largestTextureSize,
                    CalculateMipMapCount(largestTextureSize, largestTextureSize),
                    (uint)textures.Count,
                    PixelFormat.R8_G8_B8_A8_UNorm,
                    TextureUsage.Sampled));


            var commandList = game.GraphicsDevice.ResourceFactory.CreateCommandList();
            commandList.Begin();

            var i = 0;
            foreach (var texture in textures)
            {
                var sourceTexture = texture.CreateDeviceTexture(game.GraphicsDevice, game.Factory);

                for (var mipLevel = 0u; mipLevel < texture.MipLevels; mipLevel++)
                {
                    commandList.CopyTexture(
                        sourceTexture,
                        0, 0, 0,
                        mipLevel,
                        0,
                        textureArray,
                        0, 0, 0,
                        mipLevel,
                        (uint)i,
                        (uint)texture.Images[mipLevel].Width,
                        (uint)texture.Images[mipLevel].Height,
                        1,
                        1);
                }
                i++;
            }

            commandList.End();

            game.GraphicsDevice.SubmitCommands(commandList);
            game.GraphicsDevice.DisposeWhenIdle(commandList);
            game.GraphicsDevice.WaitForIdle();

            return textureArray;
        }


        private static uint CalculateMipMapCount(uint width, uint height)
        {
            return 1u + (uint)Math.Floor(Math.Log(Math.Max(width, height), 2));
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

                        var topBlock = y < HEIGHT - 1 ? blocks[x, y + 1, z] : 0;
                        if (topBlock == 0) AddFace(x, y, z, blockType - 1, FaceType.Top);

                        var bottomBlock = y > 0 ? blocks[x, y - 1, z] : 0;
                        if (bottomBlock == 0) AddFace(x, y, z, blockType - 1, FaceType.Bottom);

                        var leftBlock = x > 0 ? blocks[x - 1, y, z] : 0;
                        if (leftBlock == 0) AddFace(x, y, z, blockType - 1, FaceType.Left);

                        var rightBlock = x < WIDTH - 1 ? blocks[x + 1, y, z] : 0;
                        if (rightBlock == 0) AddFace(x, y, z, blockType - 1, FaceType.Right);

                        var backBlock = z < WIDTH - 1 ? blocks[x, y, z + 1] : 0;
                        if (backBlock == 0) AddFace(x, y, z, blockType - 1, FaceType.Back);

                        var frontBlock = z > 0 ? blocks[x, y, z - 1] : 0;
                        if (frontBlock == 0) AddFace(x, y, z, blockType - 1, FaceType.Front);
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

        private void AddFace(int x, int y, int z, int texId, FaceType face_type)
        {
            var verts = front_vertices;
            var offset = new Vector3(x, y, z);
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
