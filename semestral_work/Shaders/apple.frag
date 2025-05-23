#version 330 core
in vec3 vNormal;
in vec3 vWorldPos;
in vec2 vUv;

uniform vec3  uLightPos;
uniform vec3  uLightDir;     
uniform float uSpotCutoff;   
uniform float uLightRange;

uniform sampler2D uBaseColor;

out vec4 FragColor;

void main()
{
    vec3 albedo = texture(uBaseColor, vUv).rgb;

    vec3 N = normalize(vNormal);
    vec3 L = normalize(uLightPos - vWorldPos);

    float cosTheta = dot(normalize(-uLightDir), L);
    float spot     = clamp((cosTheta - uSpotCutoff) / (1.0 - uSpotCutoff), 0.0, 1.0);

    float dist   = length(uLightPos - vWorldPos);
    float atten  = clamp(1.0 - dist / uLightRange, 0.0, 1.0);

    float diff = max(dot(N, L), 0.0);

    float ambientStrength = 0.10;          
    float lighting        = ambientStrength + spot * atten * diff;

    vec3 color = albedo * lighting;
    FragColor  = vec4(color, 1.0);
}
