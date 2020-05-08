#version 450

layout(location = 0) flat in int fsin_texId;
layout(location = 1) in vec2 fsin_texCoords;
layout(location = 0) out vec4 fsout_color;

layout(set = 1, binding = 1) uniform texture2D TextureTop;
layout(set = 1, binding = 2) uniform texture2D TextureBottom;
layout(set = 1, binding = 3) uniform texture2D TextureLeft;
layout(set = 1, binding = 4) uniform texture2D TextureRight;
layout(set = 1, binding = 5) uniform texture2D TextureBack;
layout(set = 1, binding = 6) uniform texture2D TextureFront;
layout(set = 1, binding = 7) uniform sampler SurfaceSampler;

void main()
{
    if (fsin_texId == 0)
    {
        fsout_color = texture(sampler2D(TextureTop, SurfaceSampler), fsin_texCoords);
    }
    if (fsin_texId == 1)
    {
        fsout_color = texture(sampler2D(TextureBottom, SurfaceSampler), fsin_texCoords);
    }
    if (fsin_texId == 2)
    {
        fsout_color = texture(sampler2D(TextureLeft, SurfaceSampler), fsin_texCoords);
    }
    if (fsin_texId == 3)
    {
        fsout_color = texture(sampler2D(TextureRight, SurfaceSampler), fsin_texCoords);
    }
    if (fsin_texId == 4)
    {
        fsout_color = texture(sampler2D(TextureBack, SurfaceSampler), fsin_texCoords);
    }
    if (fsin_texId == 5)
    {
        fsout_color = texture(sampler2D(TextureFront, SurfaceSampler), fsin_texCoords);
    }
}