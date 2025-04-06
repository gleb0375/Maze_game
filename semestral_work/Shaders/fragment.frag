#version 330 core

in vec2 TexCoord;
in vec3 WorldPos;
out vec4 FragColor;

uniform sampler2D uTexture;

uniform vec3 uLightPos;   
uniform vec3 uLightDir;   
uniform float uSpotCutoff;
uniform float uLightRange;

void main()
{
    vec4 baseColor = texture(uTexture, TexCoord);
    float ambientStrength = 0.1;
    vec3 ambient = baseColor.rgb * ambientStrength;

    vec3 L = normalize(WorldPos - uLightPos);
    float theta = dot(uLightDir, L);

    float spotFactor = smoothstep(uSpotCutoff, 1.0, theta);

    spotFactor = pow(spotFactor, 1.5);

    float dist = length(WorldPos - uLightPos);
    float rangeFactor = clamp(1.0 - (dist / uLightRange), 0.0, 1.0);

    rangeFactor = pow(rangeFactor, 1.2);

    float finalFactor = spotFactor * rangeFactor;

    vec3 result = mix(ambient, baseColor.rgb, finalFactor);
    FragColor = vec4(result, baseColor.a);
}
