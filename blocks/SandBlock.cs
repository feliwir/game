using System.Numerics;

namespace game.blocks
{
    public class SandBlock : Block
    {
        public SandBlock(Vector3 position) : base(position)
        {
            _texName = "assets/sand.png";
        }
    }
}
