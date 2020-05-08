using System.Numerics;

namespace game.blocks
{
    public class DirtBlock : Block
    {
        public DirtBlock(Vector3 position) : base(position)
        {
            _texName = "assets/dirt.png";
        }
    }
}
