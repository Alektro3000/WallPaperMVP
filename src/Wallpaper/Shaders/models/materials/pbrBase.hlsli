#pragma pack_matrix(row_major)

struct LightDescription
{
    float3 lightPos;
    float invRadius;
    float4 LightColorAndFalloffExponent;   // rgb color/intensity, w = falloff
    float4 SpotAnglesAndSourceRadius;      // cone angles + source radius
    float3 LightDirection;                 // for spot / directional
    float SoftSourceRadius;
    int ShadowMapBegin;
    float3 padding;
};

#define AlbedoTexFlag 1
#define NormalTexFlag 2
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

float DistributionGGX(float3 N, float3 H, float roughness)
{
    float a = roughness * roughness;
    float a2 = a * a;

    float NdotH = saturate(dot(N, H));
    float NdotH2 = NdotH * NdotH;

    float denom = NdotH2 * (a2 - 1.0) + 1.0;
    denom = PI * denom * denom;

    return a2 / max(denom, 0.00001);
}

float GeometrySchlickGGX(float NdotV, float roughness)
{
    float r = roughness + 1.0;
    float k = (r * r) / 8.0;

    return NdotV / max(NdotV * (1.0 - k) + k, 0.00001);
}

float GeometrySmith(float3 N, float3 V, float3 L, float roughness)
{
    float NdotV = saturate(dot(N, V));
    float NdotL = saturate(dot(N, L));

    float ggxV = GeometrySchlickGGX(NdotV, roughness);
    float ggxL = GeometrySchlickGGX(NdotL, roughness);

    return ggxV * ggxL;
}

float3 FresnelSchlick(float cosTheta, float3 F0)
{
    return F0 + (1.0 - F0) * pow(saturate(1.0 - cosTheta), 5.0);
}

// ------------------------------------------------------------
// Main PBR spotlight evaluation
// ------------------------------------------------------------

float3 EvaluateSpotLightPBR(
    float3 worldPos,
    float3 N,
    float3 V,
    float3 baseColor,
    float roughness,
    float metallic,
    LightDescription light)
{
    float3 lightPos = light.lightPos;
    float3 toLight = lightPos - worldPos;
    

    float distance = length(toLight);
    float3 L = toLight / max(distance, 0.00001);


    float NdotL = saturate(dot(N, L));
    float NdotV = saturate(dot(N, V));

    if ( NdotV <= 0.0)
        return float3(0.0, 0.0, 0.0);

    if ( NdotL <= 0.0 )
        return float3(0.0, 0.0, 0.0);

    float attenuation = EvaluateSpotAttenuation(distance, L, light);

    if (attenuation <= 0.0)
        return float3(0.0, 0.0, 0.0);

    float3 H = normalize(V + L);

    float3 F0 = float3(0.04, 0.04, 0.04);
    F0 = lerp(F0, baseColor, metallic);

    float D = DistributionGGX(N, H, roughness);
    float G = GeometrySmith(N, V, L, roughness);
    float3 F = FresnelSchlick(saturate(dot(H, V)), F0);

    float3 numerator = D * G * F;
    float denominator = max(4.0 * NdotV * NdotL, 0.00001);

    float3 specular = numerator / denominator;

    float3 kS = F;
    float3 kD = 1.0 - kS;
    kD *= 1.0 - metallic;

    float3 diffuse = kD * baseColor / PI;

    float3 radiance = light.LightColorAndFalloffExponent.rgb * attenuation;

    return (diffuse + specular) * radiance * NdotL;
}
