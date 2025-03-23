#version 330 core

layout (location = 0) in vec3 aPosition; 
layout (location = 1) in vec2 aTexCoord;

out vec2 TexCoord;
out vec3 WorldPos;

uniform mat4 uModel; 
uniform mat4 uMVP;  

void main()
{
    vec4 worldPos4 = uModel * vec4(aPosition, 1.0);
    WorldPos = worldPos4.xyz;

    gl_Position = uMVP * vec4(aPosition, 1.0);

    TexCoord = aTexCoord;
}
