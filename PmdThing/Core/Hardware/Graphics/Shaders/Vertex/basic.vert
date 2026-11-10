#version 330
layout (location = 0) in vec4 vertex;

out vec2 TexCoords;

uniform vec4 atlasRect;
uniform mat4 model;
uniform mat4 projection;

void main()
{
    vec2 atlasCoord = atlasRect.xy + vertex.zw * atlasRect.zw;
    TexCoords = atlasCoord;

    gl_Position = projection * model * vec4(vertex.xy, 0.0, 1.0);
}