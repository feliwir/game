using System.Numerics;

namespace game.blocks
{
    public class OakLeavesBlock : Block
    {
        public OakLeavesBlock(Vector3 position) : base(position)
        {
            _texName = "assets/leaves_oak.png";
        }
    }
}
