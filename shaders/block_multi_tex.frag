#version 450

layout(location = 0) flat in int fsin_texId;
layout(location = 1) in vec2 fsin_texCoords;
layout(location = 0) out vec4 fsout_color;

layout(set = 1, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 2, binding = 2) uniform texture2D SurfaceTexture2;
layout(set = 1, binding = 3) uniform sampler SurfaceSampler;

void main()
{
    if (fsin_texId == 0)
    {
        fsout_color =  texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords);
    }
    else
    {
        fsout_color =  texture(sampler2D(SurfaceTexture2, SurfaceSampler), fsin_texCoords);
    }
}