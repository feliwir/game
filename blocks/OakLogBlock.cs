using System.Numerics;

namespace game.blocks
{
    public class OakLogBlock : BlockMultiTexture
    {
        public OakLogBlock(Vector3 position) : base(position)
        {
            _texNameTop = "assets/log_oak_top.png";
            _texNameBottom = "assets/log_oak_top.png";
            _texNameLeft = "assets/log_oak.png";
            _texNameRight = "assets/log_oak.png";
            _texNameBack = "assets/log_oak.png";
            _texNameFront = "assets/log_oak.png";
        }
    }
}
