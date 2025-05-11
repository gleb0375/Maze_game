#version 330 core
in vec3 vNormal;
in vec3 vWorldPos;

uniform vec3 uLightPos, uLightDir;
uniform vec3 uCamPos;

out vec4 FragColor;

void main()
{
    vec3 N  = normalize(vNormal);
    vec3 L  = normalize(uLightPos - vWorldPos);
    float NdotL = max(dot(N, L), 0.0);

    vec3 baseColor = vec3(0.9, 0.05, 0.05);
    vec3 diffuse   = baseColor * NdotL;
   
    FragColor = vec4(diffuse, 1.0);
}
