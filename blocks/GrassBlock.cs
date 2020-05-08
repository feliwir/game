using System.Numerics;

namespace game.blocks
{
    public class GrassBlock : BlockMultiTexture
    {
        public GrassBlock(Vector3 position) : base(position)
        {
            _texNameTop = "assets/grass_top.png";
            _texNameBottom = "assets/dirt.png";
            _texNameLeft = "assets/grass_side.png";
            _texNameRight = "assets/grass_side.png";
            _texNameBack = "assets/grass_side.png";
            _texNameFront = "assets/grass_side.png";
        }
    }
}
