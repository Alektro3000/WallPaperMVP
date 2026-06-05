#pragma pack_matrix(row_major)

struct LightDescription
{
    float4x4 LightViewProjMatrix;                   // for shadow mapping
    float3 lightPos;
    float invRadius;
    float4 LightColorAndFalloffExponent;   // rgb color/intensity, w = falloff
    float4 SpotAnglesAndSourceRadius;      // cone angles + source radius
    float3 LightDirection;                 // for spot / directional
    float SoftSourceRadius;
    int ShadowMapBegin;
    float3 padding;
};

#define PI 3.1415926

float EvaluateSpotAttenuation(float distance, float3 L, LightDescription light)
{   
    float distAtten = saturate(1.0 - distance * light.invRadius);
    distAtten = pow(distAtten, light.LightColorAndFalloffExponent.w);

    if(light.SpotAnglesAndSourceRadius.z != 0)
        return distAtten;

    float3 lightToPixel = -L;

    float3 spotDir = normalize(light.LightDirection);

    float cosAngle = dot(lightToPixel, spotDir);


    float cosOuter = light.SpotAnglesAndSourceRadius.x;
    float invConeDiff = light.SpotAnglesAndSourceRadius.y;


    float spotAtten = saturate((cosAngle - cosOuter) * invConeDiff);

    // Smooth edge
    spotAtten = spotAtten * spotAtten * (3.0 - 2.0 * spotAtten);
    return distAtten * spotAtten;
}

float DistributionGGX(float NdotH, float a)
{
    float a2 = a * a;

    float f = (NdotH * a2 - NdotH) * NdotH + 1.0;

    return a2 / (PI * f * f);
}

float GeometrySmith(float NdotV, float NdotL, float a)
{
    float a2 = a * a;

    float GGXL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);
    float GGXV = NdotL * sqrt((-NdotV * a2 + NdotV) * NdotV + a2);

    return 0.5 / (GGXV + GGXL);
}

float3 FresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(1.0 - cosTheta, 5.0);
}

float Fd_Lambert() {
    return 1.0 / PI;
}

float F_Schlick(float u, float f0, float f90) {
    return f0 + (f90 - f0) * pow(1.0 - u, 5.0);
}


float Fd_Burley(float NoV, float NoL, float LoH, float roughness) {
    float f90 = 0.5 + 2.0 * roughness * LoH * LoH;
    float lightScatter = F_Schlick(NoL, 1.0, f90);
    float viewScatter = F_Schlick(NoV, 1.0, f90);
    return lightScatter * viewScatter * (1.0 / PI);
}

// ------------------------------------------------------------
// Main PBR spotlight evaluation
// ------------------------------------------------------------

float3 EvaluateSpotLightPBR(
    float3 worldPos,
    float3 N,
    float3 V,
    float3 baseColor,
    float alpha,
    float metallic,
    float3 F0,
    LightDescription light)
{
    float3 lightPos = light.lightPos;
    float3 toLight = lightPos - worldPos;
    

    float distance = length(toLight);
    float3 L = toLight / max(distance, 0.00001);

    float3 H = normalize(V + L);

    float NdotL = saturate(dot(N, L));
    float NdotH = saturate(dot(N, H));
    float LdotH = saturate(dot(H, L));
    float NdotV = abs(dot(N, V)) + 1e-5;

    float attenuation = EvaluateSpotAttenuation(distance, L, light);

    if (attenuation <= 0.0)
        return float3(0.0, 0.0, 0.0);

    float D = DistributionGGX(NdotH, alpha);
    float G = GeometrySmith(NdotV, NdotL, alpha);
    float3 F = FresnelSchlick(LdotH, F0);

    float3 specular = D * G * F;

    float3 kD = (1.0 - F) * (1.0 - metallic);
    float3 diffuse = kD * baseColor * Fd_Burley(NdotV, NdotL, LdotH, alpha);

    float3 radiance = light.LightColorAndFalloffExponent.rgb * attenuation;

    return (diffuse + specular) * radiance * NdotL;
}
