
using System.Numerics;
using Veldrid;

namespace game.blocks
{
    public class GrassBlock : Block
    {
        public override string _texName { get; protected set; } = "assets/grass.jpg";

        public GrassBlock(GraphicsDevice gd, ResourceFactory factory, Swapchain sc, Vector3 position) : base(gd, factory, sc, position)
        {
        }
    }
}
