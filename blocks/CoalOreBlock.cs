using System.Numerics;

namespace game.blocks
{
    public class CoalOreBlock : Block
    {
        public CoalOreBlock(Vector3 position) : base(position)
        {
            _texName = "assets/coal_ore.png";
        }
    }
}
