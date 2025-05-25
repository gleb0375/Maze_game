#version 330 core
in vec3 vPos;
in vec3 vNorm;
in vec2 vUV;

uniform vec3  uLightPos;
uniform vec3  uLightDir;
uniform float uSpotCutoff;
uniform float uLightRange;
uniform vec3  uViewPos;

uniform sampler2D uBaseColor;

out vec4 FragColor;

void main()
{
    vec4 tex = texture(uBaseColor, vUV);   
    vec3 albedo = tex.rgb;
    float alpha = tex.a;                   

    vec3  L = uLightPos - vPos;
    float dist = length(L);
    L = normalize(L);

    float theta = dot(L, normalize(-uLightDir));
    float spot  = smoothstep(uSpotCutoff, uSpotCutoff + 0.03, theta);
    float atten = spot * clamp(1.0 - dist / uLightRange, 0.0, 1.0);

    vec3  N = normalize(vNorm);
    float diff = max(dot(N, L), 0.0);

    vec3  V = normalize(uViewPos - vPos);
    vec3  H = normalize(L + V);
    float spec = pow(max(dot(N, H), 0.0), 64.0);

    vec3 color = 0.05 * albedo                    
               + atten * (diff * albedo           
               + 0.6 * spec);                     

    if (alpha < 0.03)
        discard;

    FragColor = vec4(color, alpha);
}
