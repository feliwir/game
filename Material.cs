using System;

namespace Viking
{
    public class Material
    {
        public string DiffuseTexture { get; }
        public string NormalMap { get; }

        public Material(string diffuse = "default.png", string normal = "default.png")
        {
            DiffuseTexture = diffuse;
            NormalMap = normal;
        }

        public override bool Equals(Object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType())) return false;
            else
            {
                Material m = (Material)obj;
                return (DiffuseTexture == m.DiffuseTexture) && (NormalMap == m.NormalMap);
            }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DiffuseTexture, NormalMap);
        }
    }
}
