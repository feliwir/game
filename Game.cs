using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.ImageSharp;
using Viking.Blocks;
using Viking.Map;

namespace Viking
{
    public class Game
    {
        protected Camera _camera;

        private GameWindow m_window;
        private CommandList m_cl;
        public GraphicsDevice GraphicsDevice;
        public ResourceFactory Factory;
        public Swapchain Swapchain;

        private DeviceBuffer _projectionBuffer;
        private DeviceBuffer _viewBuffer;
        private ResourceSet _projViewSet;
        public Texture BlockDiffuseTextureArray;
        public Texture BlockNormalMapArray;

        public List<Material> BlockMaterials = new List<Material> { new Material() };
        public Dictionary<BlockType, Block> BlockTypes = new Dictionary<BlockType, Block>();
        private Dictionary<Tuple<int, int>, Chunk> Chunks = new Dictionary<Tuple<int, int>, Chunk>();

        public Game(GameWindow window)
        {
            m_window = window;
            m_window.Resized += OnResize;
            m_window.Rendering += OnPreDraw;
            m_window.Rendering += OnDraw;
            m_window.KeyPressed += OnKeyDown;
            m_window.GraphicsDeviceCreated += OnGraphicsDeviceCreated;

            _camera = new Camera(window.Width, window.Height);

            BlockTypes.Add(StoneBlock.Type, new StoneBlock(BlockMaterials));
            BlockTypes.Add(DirtBlock.Type, new DirtBlock(BlockMaterials));
            BlockTypes.Add(GrassBlock.Type, new GrassBlock(BlockMaterials));
            BlockTypes.Add(SandBlock.Type, new SandBlock(BlockMaterials));
            BlockTypes.Add(CoalOreBlock.Type, new CoalOreBlock(BlockMaterials));
            BlockTypes.Add(OakLogBlock.Type, new OakLogBlock(BlockMaterials));
            BlockTypes.Add(OakLeavesBlock.Type, new OakLeavesBlock(BlockMaterials));
        }

        public BlockType GetBlockAt(Tuple<int, int> chunkKey, int x, int y, int z)
        {
            if (!Chunks.ContainsKey(chunkKey)) return BlockType.DEFAULT;

            var chunk = Chunks[chunkKey];
            return chunk.Blocks[x, y, z];
        }

        protected void OnGraphicsDeviceCreated(GraphicsDevice gd, ResourceFactory factory, Swapchain sc)
        {
            GraphicsDevice = gd;
            Factory = factory;
            Swapchain = sc;
            CreateResources();

            Chunk.CreateResources(this);

            FastNoise noise = new FastNoise();
            noise.SetNoiseType(FastNoise.NoiseType.Simplex);

            int size = 256;
            int delta = 10;
            int[,] heightMap = new int[size, size];

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    heightMap[x, y] = (int)(noise.GetNoise(x, y) * delta);
                }
            }

            for (int x = 0; x < size; x += Chunk.WIDTH)
            {
                for (int y = 0; y < size; y += Chunk.WIDTH)
                {
                    var _chunk = new Chunk(x, y, heightMap, new Random());
                    Chunks.Add(new Tuple<int, int>(x, y), _chunk);
                }
            }
        }

        protected void CreateResources()
        {
            var blockTextures = new List<string>();
            foreach (var material in BlockMaterials) blockTextures.Add(material.DiffuseTexture);
            BlockDiffuseTextureArray = CreateBlockTextureArray(blockTextures);

            var blockNormalmaps = new List<string>();
            foreach (var material in BlockMaterials) blockNormalmaps.Add(material.NormalMap);
            BlockNormalMapArray = CreateBlockTextureArray(blockNormalmaps);

            _projectionBuffer = Factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _viewBuffer = Factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            ResourceLayout projViewLayout = Factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            _projViewSet = Factory.CreateResourceSet(new ResourceSetDescription(
                projViewLayout,
                _projectionBuffer,
                _viewBuffer));

            m_cl = Factory.CreateCommandList();
        }

        protected void OnKeyDown(KeyEvent ke)
        {
        }

        public void OnPreDraw(float delta)
        {
            _camera.Update(delta);

            Parallel.ForEach(Chunks.Values, (chunk) =>
            {
                chunk.Update(delta, this);
            });
        }

        void OnDraw(float deltaSeconds)
        {
            m_cl.Begin();

            m_cl.UpdateBuffer(_projectionBuffer, 0, _camera.ProjectionMatrix);
            m_cl.UpdateBuffer(_viewBuffer, 0, _camera.ViewMatrix);

            m_cl.SetFramebuffer(Swapchain.Framebuffer);
            m_cl.ClearColorTarget(0, RgbaFloat.Black);
            m_cl.ClearDepthStencil(1f);

            foreach (var chunk in Chunks.Values)
            {
                chunk.Draw(m_cl, _projViewSet);
            }

            m_cl.End();
            GraphicsDevice.SubmitCommands(m_cl);
            GraphicsDevice.SwapBuffers(Swapchain);
            GraphicsDevice.WaitForIdle();
        }

        public void OnResize()
        {
            _camera.WindowResized(m_window.Width, m_window.Height);
        }

        private Texture CreateBlockTextureArray(List<string> blockTextures)
        {
            var largestTextureSize = uint.MinValue;
            var textures = new List<ImageSharpTexture>();

            var defaultTexture = new ImageSharpTexture("assets/textures/default.png");

            foreach (var textureName in blockTextures)
            {
                ImageSharpTexture texture;
                try
                {
                    texture = new ImageSharpTexture("assets/textures/" + textureName);
                }
                catch(Exception)
                {
                    texture = defaultTexture;
                }
                textures.Add(texture);
                largestTextureSize = Math.Max(largestTextureSize, texture.Width);
            }

            var textureArray = GraphicsDevice.ResourceFactory.CreateTexture(
                TextureDescription.Texture2D(
                    largestTextureSize,
                    largestTextureSize,
                    CalculateMipMapCount(largestTextureSize, largestTextureSize),
                    (uint)textures.Count,
                    PixelFormat.R8_G8_B8_A8_UNorm,
                    TextureUsage.Sampled));

            // use m_cl here??
            var commandList = GraphicsDevice.ResourceFactory.CreateCommandList();
            commandList.Begin();

            var i = 0;
            foreach (var texture in textures)
            {
                var sourceTexture = texture.CreateDeviceTexture(GraphicsDevice, Factory);

                //TODO: resize texture to largestTextureSize
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

            GraphicsDevice.SubmitCommands(commandList);
            GraphicsDevice.DisposeWhenIdle(commandList);
            GraphicsDevice.WaitForIdle();

            return textureArray;
        }

        private static uint CalculateMipMapCount(uint width, uint height)
        {
            return 1u + (uint)Math.Floor(Math.Log(Math.Max(width, height), 2));
        }
    }
}
