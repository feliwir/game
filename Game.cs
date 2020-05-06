using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;
using Veldrid.SPIRV;

namespace lumos
{
    class Game
    {
        private CommandList m_cl;
        private Pipeline m_pipeline;
        private GraphicsDevice m_gd;
        private Swapchain m_mainSwapchain;

        public Game(GameWindow window)
        {
            window.Resized += OnResize;
            window.Rendering += OnPreDraw;
            window.Rendering += OnDraw;
            window.KeyPressed += OnKeyDown;
            window.GraphicsDeviceCreated += OnGraphicsDeviceCreated;
        }

        protected void OnGraphicsDeviceCreated(GraphicsDevice gd, ResourceFactory factory, Swapchain sc)
        {
            m_gd = gd;
            m_mainSwapchain = sc;
            CreateResources(factory);
        }

        protected void CreateResources(ResourceFactory factory)
        {
            string vertexCode = string.Empty;
            string fragmentCode = string.Empty;

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                        new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexCode), "main"),
                    new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentCode), "main")));

            ResourceLayout projViewLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            ResourceLayout worldTextureLayout = factory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("WorldBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("SurfaceTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            m_pipeline = factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { projViewLayout, worldTextureLayout },
                m_mainSwapchain.Framebuffer.OutputDescription));
            m_cl = factory.CreateCommandList();
        }

        protected void OnKeyDown(KeyEvent ke)
        {
        }

        public void OnPreDraw(float delta)
        {

        }

        public void OnDraw(float delta)
        {

        }

        public void OnResize()
        {

        }
    }
}
