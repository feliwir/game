#version 450

layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};

layout(set = 0, binding = 1) uniform ViewBuffer
{
    mat4 View;
};

layout(set = 1, binding = 0) uniform WorldBuffer
{
    vec3 World;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in int MaterialID;
layout(location = 2) in vec2 TexCoords;
layout(location = 3) in int FaceDirection;
layout(location = 0) out int fsin_materialId;
layout(location = 1) out vec2 fsin_texCoords;
layout(location = 2) out vec3 fsin_normalVector;
layout(location = 3) out vec3 fsin_tangentVector;
layout(location = 4) out vec3 fsin_bitangentVector;

void setNormalVectors()
{
    switch(FaceDirection)
    {
        case 0:
            fsin_normalVector = vec3(0.0, -1.0, 0.0);
            fsin_tangentVector = vec3(-1.0, 0.0, 0.0);
            fsin_bitangentVector = vec3(0.0, 0.0, -1.0);
        case 1:
            fsin_normalVector = vec3(0.0, 1.0, 0.0);
            fsin_tangentVector = vec3(1.0, 0.0, 0.0);
            fsin_bitangentVector = vec3(0.0, 0.0, 1.0);
        case 2:
            fsin_normalVector = vec3(1.0, 0.0, 0.0);
            fsin_tangentVector = vec3(0.0, 1.0, 0.0);
            fsin_bitangentVector = vec3(0.0, 0.0, 1.0);
        case 3:
            fsin_normalVector = vec3(-1.0, 0.0, 0.0);
            fsin_tangentVector = vec3(0.0, -1.0, 0.0);
            fsin_bitangentVector = vec3(0.0, 0.0, -1.0);
        case 4:
            fsin_normalVector = vec3(0.0, 0.0, 1.0);
            fsin_tangentVector = vec3(1.0, 0.0, 0.0);
            fsin_bitangentVector = vec3(0.0, 1.0, 0.0);
        case 5:
            fsin_normalVector = vec3(0.0, 0.0, -1.0);
            fsin_tangentVector = vec3(-1.0, 0.0, 0.0);
            fsin_bitangentVector = vec3(0.0, -1.0, 0.0);
    }
}

void main()
{
    vec4 worldPosition = vec4(World + Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;
    fsin_texCoords = TexCoords;
    fsin_materialId = MaterialID;
    setNormalVectors();
}