using game;
using game.blocks;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.ImageSharp;

namespace lumos
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
        public Texture BlockTextureArray;

        public List<string> BlockTextures = new List<string> { "assets/textures/default.png" };
        public Dictionary<BlockType, Block> BlockTypes = new Dictionary<BlockType, Block>();
        private List<Chunk> chunks = new List<Chunk>();

        public Game(GameWindow window)
        {
            m_window = window;
            m_window.Resized += OnResize;
            m_window.Rendering += OnPreDraw;
            m_window.Rendering += OnDraw;
            m_window.KeyPressed += OnKeyDown;
            m_window.GraphicsDeviceCreated += OnGraphicsDeviceCreated;

            _camera = new Camera(window.Width, window.Height);

            BlockTypes.Add(StoneBlock.Type, new StoneBlock(BlockTextures));
            BlockTypes.Add(DirtBlock.Type, new DirtBlock(BlockTextures));
            BlockTypes.Add(GrassBlock.Type, new GrassBlock(BlockTextures));
            BlockTypes.Add(SandBlock.Type, new SandBlock(BlockTextures));
            BlockTypes.Add(CoalOreBlock.Type, new CoalOreBlock(BlockTextures));
            BlockTypes.Add(OakLogBlock.Type, new OakLogBlock(BlockTextures));
            BlockTypes.Add(OakLeavesBlock.Type, new OakLeavesBlock(BlockTextures));
        }

        protected void OnGraphicsDeviceCreated(GraphicsDevice gd, ResourceFactory factory, Swapchain sc)
        {
            GraphicsDevice = gd;
            Factory = factory;
            Swapchain = sc;
            CreateResources();

            Chunk.CreateResources(this);
            var chunk = new Chunk(this, new Vector3(0, 0, 0), new Random());
            chunks.Add(chunk);
        }

        protected void CreateResources()
        {
            BlockTextureArray = CreateBlockTextureArray();
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

            foreach (var chunk in chunks)
            {
                chunk.Update(delta, m_cl);
            }
        }

        void OnDraw(float deltaSeconds)
        {
            m_cl.Begin();

            m_cl.UpdateBuffer(_projectionBuffer, 0, _camera.ProjectionMatrix);
            m_cl.UpdateBuffer(_viewBuffer, 0, _camera.ViewMatrix);

            m_cl.SetFramebuffer(Swapchain.Framebuffer);
            m_cl.ClearColorTarget(0, RgbaFloat.Black);
            m_cl.ClearDepthStencil(1f);

            foreach(var chunk in chunks)
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

        private Texture CreateBlockTextureArray()
        {
            var largestTextureSize = uint.MinValue;
            var textures = new List<ImageSharpTexture>();

            foreach (var textureName in BlockTextures)
            {
                var texture = new ImageSharpTexture(textureName);
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
