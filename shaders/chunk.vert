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
layout(location = 1) in int TexID;
layout(location = 2) in vec2 TexCoords;
layout(location = 3) in int FaceDirection;
layout(location = 0) out int fsin_texId;
layout(location = 1) out vec2 fsin_texCoords;
layout(location = 2) out vec3 fsin_normalVector;

vec3 getNormalVector()
{
    switch(FaceDirection)
    {
        case 0:
            return vec3(0.0,-1.0,0.0);
        case 1:
            return vec3(0.0,1.0,0.0);
        case 2:
            return vec3(1.0,0.0,0.0);
        case 3:
            return vec3(-1.0,0.0,0.0);
        case 4:
            return vec3(0.0,0.0,1.0);
        case 5:
            return vec3(0.0,0.0,-1.0);
    }
}

void main()
{
    vec4 worldPosition = vec4(World + Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;
    fsin_texCoords = TexCoords;
    fsin_texId = TexID;
    fsin_normalVector = getNormalVector();
}