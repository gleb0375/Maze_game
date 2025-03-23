#version 330 core

in vec2 TexCoord;
in vec3 WorldPos;
out vec4 FragColor;

uniform sampler2D uTexture;

uniform vec3 uLightPos;   
uniform vec3 uLightDir;   
uniform float uSpotCutoff;

void main()
{
    vec4 baseColor = texture(uTexture, TexCoord);
    
    float ambientStrength = 0.1;
    vec3 ambient = baseColor.rgb * ambientStrength;
    
    vec3 L = normalize(WorldPos - uLightPos);
    float theta = dot(uLightDir, L);
    
    float intensity = smoothstep(uSpotCutoff, 1.0, theta);
    
    vec3 result = mix(ambient, baseColor.rgb, intensity);
    FragColor = vec4(result, baseColor.a);
}
