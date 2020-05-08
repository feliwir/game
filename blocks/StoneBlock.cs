
using System.Numerics;
using Veldrid;

namespace game.blocks
{
    public class StoneBlock : Block
    {
        public override string _texName { get; protected set; } = "assets/stone.png";

        public StoneBlock(GraphicsDevice gd, ResourceFactory factory, Swapchain sc, Vector3 position) : base(gd, factory, sc, position)
        {
        }
    }
}
