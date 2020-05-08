using game;
using game.blocks;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;

namespace lumos
{
    class Game
    {
        protected Camera _camera;

        private CommandList m_cl;
        private GraphicsDevice m_gd;
        private ResourceFactory m_factory;
        private GameWindow m_window;
        private Swapchain m_sc;

        private DeviceBuffer _projectionBuffer;
        private DeviceBuffer _viewBuffer;
        private ResourceSet _projViewSet;

        private List<Block> blocks = new List<Block>();

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
            m_gd = gd;
            m_factory = factory;
            m_sc = sc;
            CreateResources();

            for (var x = 0; x < 16; x++)
            {
                for (var z = 0; z < 16; z++)
                {
                    blocks.Add(new StoneBlock(gd, factory, sc, new Vector3(x, 0, z)));
                }
            }

            for (var x = 0; x < 16; x++)
            {
                for (var z = 0; z < 16; z++)
                {
                    blocks.Add(new DirtBlock(gd, factory, sc, new Vector3(x, 1, z)));
                }
            }

            for (var x = 0; x < 16; x++)
            {
                for (var z = 0; z < 16; z++)
                {
                    blocks.Add(new DirtBlock(gd, factory, sc, new Vector3(x, 2, z)));
                }
            }

            //for (var x = 0; x < 16; x++)
            //{
            //    for (var z = 0; z < 16; z++)
            //    {
            //        blocks.Add(new GrassBlock(gd, factory, sc, new Vector3(x, 2, z)));
            //    }
            //}
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
        }

        protected void OnKeyDown(KeyEvent ke)
        {
        }

        public void OnPreDraw(float delta)
        {
            _camera.Update(delta);

            foreach (var block in blocks)
            {
                block.Update(delta, m_cl);
            }
        }

        void OnDraw(float deltaSeconds)
        {
            m_cl.Begin();

            m_cl.UpdateBuffer(_projectionBuffer, 0, _camera.ProjectionMatrix);
            m_cl.UpdateBuffer(_viewBuffer, 0, _camera.ViewMatrix);

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
            _camera.WindowResized(m_window.Width, m_window.Height);
        }
    }
}
