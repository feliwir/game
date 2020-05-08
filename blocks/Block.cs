
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;

namespace game
{
    public abstract class Block
    {
        public virtual string _texName { get; protected set; }

        protected ResourceSet _worldTextureSet;

        protected DeviceBuffer _vertexBuffer;
        protected DeviceBuffer _indexBuffer;

        protected DeviceBuffer m_worldBuffer;

        protected Pipeline m_pipeline;

        private readonly VertexPositionTexture[] _vertices;
        protected readonly ushort[] _indices;

        private Matrix4x4 m_worldMatrix;

        public Block(GraphicsDevice gd, ResourceFactory factory, Swapchain sc, Vector3 position)
        {
            m_worldMatrix = Matrix4x4.Identity;
            m_worldMatrix.Translation = position;
            _vertices = GetVertices();
            _indices = GetIndices();
            CreateResources(gd, factory, sc);
        }

        protected void CreateResources(GraphicsDevice gd, ResourceFactory factory, Swapchain sc)
        {
            m_worldBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            _vertexBuffer = factory.CreateBuffer(new BufferDescription((uint)(VertexPositionTexture.SizeInBytes * _vertices.Length), BufferUsage.VertexBuffer));
            gd.UpdateBuffer(_vertexBuffer, 0, _vertices);

            _indexBuffer = factory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)_indices.Length, BufferUsage.IndexBuffer));
            gd.UpdateBuffer(_indexBuffer, 0, _indices);

            var texData = new ImageSharpTexture(_texName);
            var surfaceTexture = texData.CreateDeviceTexture(gd, factory);

            var vertexShaderCode = System.IO.File.ReadAllText("shaders/block.vert");
            var fragmentShaderCode = System.IO.File.ReadAllText("shaders/block.frag");

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                        new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
                factory.CreateFromSpirv(
                    new ShaderDescription(ShaderStages.Vertex, Encoding.UTF8.GetBytes(vertexShaderCode), "main"),
                    new ShaderDescription(ShaderStages.Fragment, Encoding.UTF8.GetBytes(fragmentShaderCode), "main")));

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
                sc.Framebuffer.OutputDescription));

            _worldTextureSet = factory.CreateResourceSet(new ResourceSetDescription(
                worldTextureLayout,
                m_worldBuffer,
                surfaceTexture,
                gd.Aniso4xSampler));
        }

        public void Update(float deltaSeconds, CommandList cl)
        {
            cl.UpdateBuffer(m_worldBuffer, 0, ref m_worldMatrix);
        }

        public void Draw(CommandList cl, ResourceSet projViewSet)
        {
            cl.SetPipeline(m_pipeline);
            cl.SetVertexBuffer(0, _vertexBuffer);
            cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
            cl.SetGraphicsResourceSet(0, projViewSet);
            cl.SetGraphicsResourceSet(1, _worldTextureSet);
            cl.DrawIndexed(36, 1, 0, 0, 0);
        }

        private static VertexPositionTexture[] GetVertices()
        {
            VertexPositionTexture[] vertices = new VertexPositionTexture[]
            {
                // Top
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 1)),
                // Bottom
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f, +0.5f),  new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f,-0.5f, +0.5f),  new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f,-0.5f, -0.5f),  new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f, -0.5f),  new Vector2(0, 1)),
                // Left 
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0, 1)),
                // Right
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(0, 1)),
                // Back
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, -0.5f), new Vector2(0, 1)),
                // Front
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, +0.5f), new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, +0.5f), new Vector2(0, 1)),
            };

            return vertices;
        }

        private static ushort[] GetIndices()
        {
            ushort[] indices =
            {
                0,1,2, 0,2,3,
                4,5,6, 4,6,7,
                8,9,10, 8,10,11,
                12,13,14, 12,14,15,
                16,17,18, 16,18,19,
                20,21,22, 20,22,23,
            };

            return indices;
        }

        private struct VertexPositionTexture
        {
            public const uint SizeInBytes = 20;

            public float PosX;
            public float PosY;
            public float PosZ;

            public float TexU;
            public float TexV;

            public VertexPositionTexture(Vector3 pos, Vector2 uv)
            {
                PosX = pos.X;
                PosY = pos.Y;
                PosZ = pos.Z;
                TexU = uv.X;
                TexV = uv.Y;
            }
        }
    }
}
