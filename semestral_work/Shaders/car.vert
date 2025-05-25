#version 330 core
layout(location = 0) in vec3 aPos;
layout(location = 1) in vec3 aNormal;
layout(location = 2) in vec2 aTex;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec3 vPos;
out vec3 vNorm;
out vec2 vUV;

void main()
{
    vec4 world = uModel * vec4(aPos,1.0);
    vPos  = world.xyz;
    vNorm = mat3(transpose(inverse(uModel))) * aNormal;
    vUV   = aTex;
    gl_Position = uProjection * uView * world;
}