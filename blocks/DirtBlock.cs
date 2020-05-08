
using System.Numerics;
using Veldrid;

namespace game.blocks
{
    public class DirtBlock : Block
    {
        public override string _texName { get; protected set; } = "assets/dirt.png";

        public DirtBlock(GraphicsDevice gd, ResourceFactory factory, Swapchain sc, Vector3 position) : base(gd, factory, sc, position)
        {
        }
    }
}
