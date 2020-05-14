using System.Collections.Generic;
using System.Numerics;

namespace Viking.Map
{
    public class MeshGenerator
    {
        private List<ushort> m_indices;
        private List<VertexType> m_vertices;

        public MeshGenerator()
        {
            m_indices = new List<ushort>();
            m_vertices = new List<VertexType>();
        }

        internal List<ushort> Indices => m_indices;
        internal List<VertexType> Vertices => m_vertices;

        void Reset()
        {
            m_indices.Clear();
            m_vertices.Clear();
        }

        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, int d_u, int d_v, int matID)
        {
            // TODO: get face direction, blockType and compute uvs
            int i = m_vertices.Count;
            var uv_scale = new Vector2(d_v, d_u);

            m_vertices.Add(new VertexType(v1, matID, UvCoords[0] * uv_scale, Direction.UP));
            m_vertices.Add(new VertexType(v2, matID, UvCoords[1] * uv_scale, Direction.UP));
            m_vertices.Add(new VertexType(v3, matID, UvCoords[2] * uv_scale, Direction.UP));
            m_vertices.Add(new VertexType(v4, matID, UvCoords[3] * uv_scale, Direction.UP));

            m_indices.Add((ushort)(i + 0));
            m_indices.Add((ushort)(i + 2));
            m_indices.Add((ushort)(i + 1));
            m_indices.Add((ushort)(i + 2));
            m_indices.Add((ushort)(i + 0));
            m_indices.Add((ushort)(i + 3));
        }

        private static List<Vector3> TopVertices = new List<Vector3>
        {
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 1f, 1f),
            new Vector3(0f, 1f, 1f)
        };

        private static List<Vector3> BottomVertices = new List<Vector3>
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 0f, 1f),
            new Vector3(1f, 0f, 1f)
        };

        private static List<Vector3> WestVertices = new List<Vector3>
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 1f, 1f),
            new Vector3(0f, 0f, 1f)
        };

        private static List<Vector3> EastVertices = new List<Vector3>
        {
            new Vector3(1f, 0f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(1f, 1f, 0f),
            new Vector3(1f, 0f, 0f)
        };

        private static List<Vector3> NorthVertices = new List<Vector3>
        {
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(0f, 0f, 0f)
        };

        private static List<Vector3> SouthVertices = new List<Vector3>
        {
            new Vector3(0f, 0f, 1f),
            new Vector3(0f, 1f, 1f),
            new Vector3(1f, 1f, 1f),
            new Vector3(1f, 0f, 1f)
        };

        private static List<Vector2> UvCoords = new List<Vector2>
        {
            // using 1.0 causes the grass_side texture to cause a small green stripe
            new Vector2(0, 0.99f),
            new Vector2(0, 0),
            new Vector2(0.99f, 0),
            new Vector2(0.99f, 0.99f),
        };
    }
}
