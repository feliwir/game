
using lumos;
using System.Numerics;
using System.Text;
using Veldrid;
using Veldrid.ImageSharp;
using Veldrid.SPIRV;

namespace game.blocks
{
    public abstract class BlockMultiTexture : Block
    {
        protected string _texNameTop = "assets/default.png";
        protected string _texNameBottom = "assets/default.png";
        protected string _texNameLeft = "assets/default.png";
        protected string _texNameRight = "assets/default.png";
        protected string _texNameBack = "assets/default.png";
        protected string _texNameFront = "assets/default.png";

        private readonly VertexPositionTexture[] _vertices;

        public BlockMultiTexture(Vector3 position) : base(position)
        {
            _vertices = GetVertices();
        }

        public override void CreateResources(Game game)
        {
            m_worldBuffer = game.Factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            _vertexBuffer = game.Factory.CreateBuffer(new BufferDescription((uint)(VertexPositionTexture.SizeInBytes * _vertices.Length), BufferUsage.VertexBuffer));
            game.GraphicsDevice.UpdateBuffer(_vertexBuffer, 0, _vertices);

            _indexBuffer = game.Factory.CreateBuffer(new BufferDescription(sizeof(ushort) * (uint)_indices.Length, BufferUsage.IndexBuffer));
            game.GraphicsDevice.UpdateBuffer(_indexBuffer, 0, _indices);

            var textureTop = new ImageSharpTexture(_texNameTop).CreateDeviceTexture(game.GraphicsDevice, game.Factory);
            var textureBottom = new ImageSharpTexture(_texNameBottom).CreateDeviceTexture(game.GraphicsDevice, game.Factory);
            var textureLeft = new ImageSharpTexture(_texNameLeft).CreateDeviceTexture(game.GraphicsDevice, game.Factory);
            var textureRight = new ImageSharpTexture(_texNameRight).CreateDeviceTexture(game.GraphicsDevice, game.Factory);
            var textureBack = new ImageSharpTexture(_texNameBack).CreateDeviceTexture(game.GraphicsDevice, game.Factory);
            var textureFront = new ImageSharpTexture(_texNameFront).CreateDeviceTexture(game.GraphicsDevice, game.Factory);

            var vertexShaderCode = System.IO.File.ReadAllText("shaders/block_multi_tex.vert");
            var fragmentShaderCode = System.IO.File.ReadAllText("shaders/block_multi_tex.frag");

            ShaderSetDescription shaderSet = new ShaderSetDescription(
                new[]
                {
                    new VertexLayoutDescription(
                        new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                        new VertexElementDescription("TexID", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Int1),
                        new VertexElementDescription("TexCoords", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2))
                },
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
                    new ResourceLayoutElementDescription("TextureTop", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureBottom", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureLeft", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureRight", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureBack", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("TextureFront", ResourceKind.TextureReadOnly, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("SurfaceSampler", ResourceKind.Sampler, ShaderStages.Fragment)));

            m_pipeline = game.Factory.CreateGraphicsPipeline(new GraphicsPipelineDescription(
                BlendStateDescription.SingleOverrideBlend,
                DepthStencilStateDescription.DepthOnlyLessEqual,
                RasterizerStateDescription.Default,
                PrimitiveTopology.TriangleList,
                shaderSet,
                new[] { projViewLayout, worldTextureLayout },
                game.Swapchain.Framebuffer.OutputDescription));

            _worldTextureSet = game.Factory.CreateResourceSet(new ResourceSetDescription(
                worldTextureLayout,
                m_worldBuffer,
                textureTop,
                textureBottom,
                textureLeft,
                textureRight,
                textureBack,
                textureFront,
                game.GraphicsDevice.Aniso4xSampler));
        }

        public new void Draw(CommandList cl, ResourceSet projViewSet)
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
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), 0, new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), 0, new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), 0, new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), 0, new Vector2(0, 1)),
                // Bottom
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f, +0.5f), 1, new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f,-0.5f, +0.5f), 1, new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f,-0.5f, -0.5f), 1, new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f,-0.5f, -0.5f), 1, new Vector2(0, 1)),
                // Left 
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), 2, new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), 2, new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, +0.5f), 2, new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), 2, new Vector2(0, 1)),
                // Right
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), 3, new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), 3, new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, -0.5f), 3, new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, +0.5f), 3, new Vector2(0, 1)),
                // Back
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, -0.5f), 4, new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, -0.5f), 4, new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, -0.5f), 4, new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, -0.5f), 4, new Vector2(0, 1)),
                // Front
                new VertexPositionTexture(new Vector3(-0.5f, +0.5f, +0.5f), 5, new Vector2(0, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, +0.5f, +0.5f), 5, new Vector2(1, 0)),
                new VertexPositionTexture(new Vector3(+0.5f, -0.5f, +0.5f), 5, new Vector2(1, 1)),
                new VertexPositionTexture(new Vector3(-0.5f, -0.5f, +0.5f), 5, new Vector2(0, 1)),
            };

            return vertices;
        }

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
