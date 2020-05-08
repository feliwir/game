using game;
using game.blocks;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace lumos
{
    class Game
    {
        private CommandList m_cl;
        private GraphicsDevice m_gd;
        private ResourceFactory m_factory;
        private GameWindow m_window;
        private Swapchain m_sc;

        private DeviceBuffer _projectionBuffer;
        private DeviceBuffer _viewBuffer;
        private ResourceSet _projViewSet;

        private Matrix4x4 m_fov;
        private Matrix4x4 m_lookAt;

        private List<Block> blocks = new List<Block>();

        public Game(GameWindow window)
        {
            m_window = window;
            m_window.Resized += OnResize;
            m_window.Rendering += OnPreDraw;
            m_window.Rendering += OnDraw;
            m_window.KeyPressed += OnKeyDown;
            m_window.GraphicsDeviceCreated += OnGraphicsDeviceCreated;
        }

        protected void OnGraphicsDeviceCreated(GraphicsDevice gd, ResourceFactory factory, Swapchain sc)
        {
            m_gd = gd;
            m_factory = factory;
            m_sc = sc;
            CreateResources();

            m_fov = Matrix4x4.CreatePerspectiveFieldOfView(
                1.0f,
                (float)m_window.Width / m_window.Height,
                0.5f,
                100f);

            m_lookAt = Matrix4x4.CreateLookAt(Vector3.UnitZ * 2.5f, Vector3.Zero, Vector3.UnitY);

            for (var x = 0; x < 5; x++)
            {
                for (var y = 0; y < 5; y++)
                {
                    blocks.Add(new GrassBlock(gd, factory, sc, new Vector3(x, y, 0)));
                }
            }
        }

        protected void CreateResources()
        {
            _projectionBuffer = m_factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            _viewBuffer = m_factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            ResourceLayout projViewLayout = m_factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            _projViewSet = m_factory.CreateResourceSet(new ResourceSetDescription(
                projViewLayout,
                _projectionBuffer,
                _viewBuffer));

            m_cl = m_factory.CreateCommandList();

            m_cl.UpdateBuffer(_projectionBuffer, 0, ref m_fov);
            m_cl.UpdateBuffer(_viewBuffer, 0, ref m_lookAt);
        }

        protected void OnKeyDown(KeyEvent ke)
        {
        }

        public void OnPreDraw(float delta)
        {
            foreach (var block in blocks)
            {
                block.Update(delta, m_cl);
            }
        }

        void OnDraw(float deltaSeconds)
        {
            m_cl.Begin();

            m_cl.UpdateBuffer(_projectionBuffer, 0, ref m_fov);
            m_cl.UpdateBuffer(_viewBuffer, 0, ref m_lookAt);

            m_cl.SetFramebuffer(m_sc.Framebuffer);
            m_cl.ClearColorTarget(0, RgbaFloat.Black);
            m_cl.ClearDepthStencil(1f);

            foreach(var block in blocks)
            {
                block.Draw(m_cl, _projViewSet);
            }

            m_cl.End();
            m_gd.SubmitCommands(m_cl);
            m_gd.SwapBuffers(m_sc);
            m_gd.WaitForIdle();
        }

        public void OnResize()
        {
            m_fov = Matrix4x4.CreatePerspectiveFieldOfView(
                1.0f,
                (float)m_window.Width / m_window.Height,
                0.5f,
                100f);

            m_cl.UpdateBuffer(_projectionBuffer, 0, ref m_fov);
        }
    }
}
