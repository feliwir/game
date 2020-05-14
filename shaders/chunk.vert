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
layout(location = 2) out vec3 fsin_ld_ts;

mat3 getTBNMatrix()
{
    vec3 normal = vec3(0);
    vec3 tangent = vec3(1);

    switch(FaceDirection)
    {
        case 0:
            normal = vec3(0.0, 1.0, 0.0);
            tangent = vec3(1.0, 0.0, 0.0);
        case 1:
            normal = vec3(0.0, -1.0, 0.0);
            tangent = vec3(-1.0, 0.0, 0.0);
        case 2:
            normal = vec3(1.0, 0.0, 0.0);
            tangent = vec3(0.0, 1.0, 0.0);
        case 3:
            normal = vec3(-1.0, 0.0, 0.0);
            tangent = vec3(0.0, -1.0, 0.0);
        case 4:
            normal = vec3(0.0, 0.0, 1.0);
            tangent = vec3(1.0, 0.0, 0.0);
        case 5:
            normal = vec3(0.0, 0.0, -1.0);
            tangent = vec3(-1.0, 0.0, 0.0);
    }

    vec3 bitangent = cross(normal, tangent);
    return mat3(tangent, bitangent, normal);
}

void main()
{
    vec4 worldPosition = vec4(World + Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;
    fsin_texCoords = TexCoords;
    fsin_materialId = MaterialID;
    mat3 tbn = getTBNMatrix();
    vec3 ld_cs =  normalize(vec3(0.2, -0.7, 0.2));
    fsin_ld_ts = tbn * ld_cs;
}