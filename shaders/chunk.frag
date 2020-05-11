#version 450

layout(location = 0) flat in int fsin_materialId;
layout(location = 1) in vec2 fsin_texCoords;
layout(location = 2) in vec3 fsin_normalVector;
layout(location = 0) out vec4 fsout_color;

layout(set = 1, binding = 1) uniform texture2DArray DiffuseTexture;
layout(set = 1, binding = 2) uniform texture2DArray NormalMap;
layout(set = 1, binding = 3) uniform sampler SurfaceSampler;

void main()
{
    // Light
    vec3 lightDir = vec3(0.0, -1.0, 0.5);
    lightDir = normalize(lightDir);
    vec3 lightColor = vec3(0.9, 1.0, 0.9);

    // Material
    vec4 blockColor = texture(sampler2DArray(DiffuseTexture, SurfaceSampler), vec3(fsin_texCoords, fsin_materialId));

    // calculate ambient term
    float ambientStrength = 0.2;
    vec3 ambient = ambientStrength * lightColor;

    // calculate diffuse term
    float diff = 0.8 * max(dot(fsin_normalVector, lightDir), 0.0);
    vec3 diffuse = diff * lightColor;

    vec3 shadingColor = ambient + diffuse;
    fsout_color = vec4(shadingColor, 1.0) * blockColor;
}