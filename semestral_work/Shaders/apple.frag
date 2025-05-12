#version 330 core
in vec3 vNormal;
in vec3 vWorldPos;
in vec2 vUv;

uniform vec3  uLightPos;
uniform vec3  uLightDir;
uniform float uSpotCutoff;
uniform float uLightRange;

uniform vec3      uViewPos;
uniform sampler2D uBaseColor;

out vec4 FragColor;

void main()
{
    vec3 albedo = texture(uBaseColor, vUv).rgb;

    vec3 N = normalize(vNormal);
    vec3 L = normalize(uLightPos - vWorldPos);

    float spot = dot(normalize(-uLightDir), L);
    if (spot < uSpotCutoff) discard;

    float dist   = length(uLightPos - vWorldPos);
    float atten  = clamp(1.0 - dist / uLightRange, 0.0, 1.0);

    float diff = max(dot(N, L), 0.0) * atten;

    vec3 ambient = 0.15 * albedo;
    vec3 color   = ambient + albedo * diff;

    FragColor = vec4(color, 1.0);
}
