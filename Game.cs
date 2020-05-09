using game;
using game.blocks;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

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
        }

        protected void OnGraphicsDeviceCreated(GraphicsDevice gd, ResourceFactory factory, Swapchain sc)
        {
            GraphicsDevice = gd;
            Factory = factory;
            Swapchain = sc;
            CreateResources();

            var chunk = new Chunk(this, new Vector3(0, 0, 0));
            chunks.Add(chunk);
        }

        protected void CreateResources()
        {
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
    }
}
