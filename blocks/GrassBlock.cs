using System.Numerics;
using Veldrid;

namespace game.blocks
{
    public class GrassBlock : BlockMultiTexture
    {
        public override string _texName { get; protected set; } = "assets/grass_top.png";
        public override string _texName2 { get; protected set; } = "assets/grass_side.png";

        public GrassBlock(GraphicsDevice gd, ResourceFactory factory, Swapchain sc, Vector3 position) : base(gd, factory, sc, position)
        {
        }
    }
}
