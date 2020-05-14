#version 450

layout(location = 0) flat in int fsin_materialId;
layout(location = 1) in vec2 fsin_texCoords;
layout(location = 2) in vec3 fsin_ld_ts;
layout(location = 0) out vec4 fsout_color;

layout(set = 1, binding = 1) uniform texture2DArray DiffuseTexture;
layout(set = 1, binding = 2) uniform texture2DArray NormalMap;
layout(set = 1, binding = 3) uniform sampler SurfaceSampler;

void main()
{
    // Light
    vec3 lightDir = normalize(fsin_ld_ts);
    vec3 lightColor = vec3(0.9, 1.0, 0.9);

    // Material
    vec4 blockColor = texture(sampler2DArray(DiffuseTexture, SurfaceSampler), vec3(fsin_texCoords, fsin_materialId));

    // calculate ambient term
    float ambientStrength = 0.2;
    vec3 ambient = ambientStrength * lightColor;

    // obtain normal from normal map in range [0,1]
    vec3 normal = texture(sampler2DArray(NormalMap, SurfaceSampler), vec3(fsin_texCoords, fsin_materialId)).rgb;
    // transform normal vector to range [-1,1]
    normal = normalize(normal * 2.0 - 1.0);   

    // calculate diffuse term
    float diff = 0.8 * clamp(dot(normal, lightDir), 0.0, 1.0);
    vec3 diffuse = diff * lightColor;

    vec3 shadingColor = ambient + diffuse;
    fsout_color = vec4(shadingColor, 1.0);// * blockColor;
}