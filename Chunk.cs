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

            for (var _y = 0; _y < HEIGHT; _y++)
            {
                for (var _x = 0; _x < WIDTH; _x++)
                {
                    for (var _z = 0; _z < WIDTH; _z++)
                    {
                        Blocks[_x, _y, _z] = BlockType.NONE;
                    }
                }
            }

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
            ReduceMesh(game);

            vertexBuffer = game.Factory.CreateBuffer(new BufferDescription((uint)(VertexType.SizeInBytes * vertices.Count), BufferUsage.VertexBuffer));
            game.GraphicsDevice.UpdateBuffer(vertexBuffer, 0, vertices.ToArray());

            indexBuffer = game.Factory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)indices.Count, BufferUsage.IndexBuffer));
            game.GraphicsDevice.UpdateBuffer(indexBuffer, 0, indices.ToArray());
        }

        private BlockType GetBlockAt(int x, int y, int z, Game game)
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

        public void ReduceMesh(Game game)
        {
            int[] dimensions = { WIDTH, HEIGHT, WIDTH };

            //Sweep over 3-axes
            for (var direction = 0; direction < 3; direction++)
            {
                int w = 0;
                int h = 0;

                int u = (direction + 1) % 3;
                int v = (direction + 2) % 3;

                int[] x = { 0, 0, 0 };
                int[] q = { 0, 0, 0 };
                int[] mask = new int[(dimensions[u] + 1) * (dimensions[v] + 1)];


                q[direction] = 1;

                for (x[direction] = -1; x[direction] < dimensions[direction];)
                {
                    // Compute the mask
                    int n = 0;
                    for (x[v] = 0; x[v] < dimensions[v]; ++x[v])
                    {
                        for (x[u] = 0; x[u] < dimensions[u]; ++x[u], ++n)
                        {
                            int vox1 = (int)GetBlockAt(x[0], x[1], x[2], game);
                            int vox2 = (int)GetBlockAt(x[0] + q[0], x[1] + q[1], x[2] + q[2], game);

                            int a = 0 <= x[direction] ? vox1 : 0;
                            int b = x[direction] < dimensions[direction] - 1 ? vox2 : 0;

                            if ((a != 0) == (b != 0)) mask[n] = 0;
                            else if (a != 0) mask[n] = a;
                            else mask[n] = -b;
                        }
                    }

                    ++x[direction];

                    // Generate mesh for mask using lexicographic ordering
                    n = 0;
                    for (var j = 0; j < dimensions[v]; ++j)
                    {
                        for (var i = 0; i < dimensions[u];)
                        {
                            var block_type = mask[n];

                            if (block_type == 0)
                            {
                                ++i;
                                ++n;
                                continue;
                            }

                            // compute width
                            for (w = 1; mask[n + w] == block_type && (i + w) < dimensions[u]; ++w) { }

                            // compute height
                            bool done = false;
                            for (h = 1; (j + h) < dimensions[v]; ++h)
                            {
                                for (var k = 0; k < w; ++k)
                                {
                                    if (mask[n + k + h * dimensions[u]] != block_type)
                                    {
                                        done = true;
                                        break;
                                    }
                                }
                                if (done)
                                {
                                    break;
                                }
                            }

                            // add quad
                            x[u] = i;
                            x[v] = j;

                            int[] du = { 0, 0, 0 };
                            int[] dv = { 0, 0, 0 };

                            if (block_type > 0)
                            {
                                dv[v] = h;
                                du[u] = w;
                            }
                            else
                            {
                                du[v] = h;
                                dv[u] = w;
                            }

                            Vector3 v1 = new Vector3(x[0], x[1], x[2]);
                            Vector3 v2 = new Vector3(x[0] + du[0], x[1] + du[1], x[2] + du[2]);
                            Vector3 v3 = new Vector3(x[0] + du[0] + dv[0], x[1] + du[1] + dv[1], x[2] + du[2] + dv[2]);
                            Vector3 v4 = new Vector3(x[0] + dv[0], x[1] + dv[1], x[2] + dv[2]);

                            var d_u = 0;
                            var d_v = 0;
                            for (var r = 0; r < 3; r++)
                            {
                                if (du[r] > d_u) d_u = du[r];
                                if (dv[r] > d_v) d_v = dv[r];
                            }

                            var dir = direction;
                            if (block_type < 0)
                            {
                                dir = Math.Abs(block_type) + 3;
                            }

                            var block = game.BlockTypes[(BlockType)Math.Abs(block_type)];
                            AddQuad(v1, v2, v3, v4, d_u, d_v, block.GetMaterialID((Direction)dir));

                            for (var l = 0; l < h; ++l)
                            {
                                for (var k = 0; k < w; ++k)
                                {
                                    mask[n + k + l * dimensions[u]] = 0;
                                }
                            }

                            i += w;
                            n += w;
                        }
                    }
                }
            }
        }

        private void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, int d_u, int d_v, int matID)
        {
            // TODO: get face direction, blockType and compute uvs
            int i = vertices.Count;
            var uv_scale = new Vector2(d_v, d_u);

            vertices.Add(new VertexType(v1, matID, uv_coords[0] * uv_scale, Direction.TOP));
            vertices.Add(new VertexType(v2, matID, uv_coords[1] * uv_scale, Direction.TOP));
            vertices.Add(new VertexType(v3, matID, uv_coords[2] * uv_scale, Direction.TOP));
            vertices.Add(new VertexType(v4, matID, uv_coords[3] * uv_scale, Direction.TOP));

            indices.Add((ushort)(i + 0));
            indices.Add((ushort)(i + 2));
            indices.Add((ushort)(i + 1));
            indices.Add((ushort)(i + 2));
            indices.Add((ushort)(i + 0));
            indices.Add((ushort)(i + 3));
            
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
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0f, 0f)
        };

        private static List<Vector3> south_vertices = new List<Vector3>
        {
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 1f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(1f, 0f, 1f)
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
