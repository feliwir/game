using System.Numerics;
using System.Runtime.InteropServices;

namespace Viking.Map
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VertexType
    {
        public const uint SizeInBytes = 28;

        public Vector3 Position;
        public int MaterialID;
        public Vector2 TexCoords;
        public int FaceDirection;

        public VertexType(Vector3 pos, int matId, Vector2 uv, Direction faceDir)
        {
            Position = pos;
            MaterialID = matId;
            TexCoords = uv;
            FaceDirection = (int)faceDir;
        }
    }
}
