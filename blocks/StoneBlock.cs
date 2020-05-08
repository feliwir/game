using System.Numerics;

namespace game.blocks
{
    public class StoneBlock : Block
    {
        public StoneBlock(Vector3 position) : base(position)
        {
            _texName = "assets/stone.png";
        }
    }
}
