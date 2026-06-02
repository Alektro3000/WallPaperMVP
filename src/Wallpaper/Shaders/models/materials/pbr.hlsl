#include "modelBase.hlsli"

cbuffer MaterialBuffer : register(b1)
{
    float3 albedoColor;
    int albedoTextureIndex;
    int normalTextureIndex;
    int packedTextureIndex;
    uint flags;
	float Metallic;
	float Roughness;
	float AmbientIntensity;
};

#define AlbedoTexFlag 1
#define NormalTexFlag 2
#define PackedTexFlag 4

Texture2D TextureHeap[] : register(t0);
SamplerState LinearSampler : register(s0);

#ifdef SKELETAL
cbuffer JointsConstants : register(b2)
{
    float4x4 Joints[1024];
}
#endif

struct VSInput
{
    float3 position : POSITION;
    float3 normal   : NORMAL;
    float4 tangent  : TANGENT;
    float2 UV       : TEXCOORD;

#ifdef SKELETAL
    float4 Weights  : WEIGHTS;
    uint4  Joints   : JOINTS;
#endif
};

struct VSOutput
{
    float4 Position : SV_Position;
    float3 worldPosition : TEXCOORD0;
    float3 normal   : TEXCOORD1;
    float4 tangent  : TEXCOORD2;
    float2 UV       : TEXCOORD3;
};

float3 TonemapACES(float3 x)
{
    float a = 2.51;
    float b = 0.03;
    float c = 2.43;
    float d = 0.59;
    float e = 0.14;

    return saturate((x * (a * x + b)) / (x * (c * x + d) + e));
}

VSOutput MAIN_VS(VSInput input)
{
    VSOutput o;

    float4 localPosition = float4(input.position, 1.0);
    float4 localNormal = float4(input.normal, 0.0);
    float4 localTangent = float4(input.tangent.xyz, 0.0f);

#ifdef SKELETAL
    float4 skinnedPosition =
        mul(localPosition, Joints[input.Joints.x]) * input.Weights.x +
        mul(localPosition, Joints[input.Joints.y]) * input.Weights.y +
        mul(localPosition, Joints[input.Joints.z]) * input.Weights.z +
        mul(localPosition, Joints[input.Joints.w]) * input.Weights.w;
    float3 skinnedNormal = normalize(
        mul(localNormal, Joints[input.Joints.x]) * input.Weights.x +
        mul(localNormal, Joints[input.Joints.y]) * input.Weights.y +
        mul(localNormal, Joints[input.Joints.z]) * input.Weights.z +
        mul(localNormal, Joints[input.Joints.w]) * input.Weights.w).xyz;
    float3 skinnedTangent = normalize(
        mul(localTangent, Joints[input.Joints.x]) * input.Weights.x +
        mul(localTangent, Joints[input.Joints.y]) * input.Weights.y +
        mul(localTangent, Joints[input.Joints.z]) * input.Weights.z +
        mul(localTangent, Joints[input.Joints.w]) * input.Weights.w).xyz;
#else
    float4 skinnedPosition = localPosition;
    float3 skinnedNormal = localNormal.xyz;
    float3 skinnedTangent = localTangent.xyz;
#endif

    float4 worldPosition = mul(skinnedPosition, ModelTransform);
    o.worldPosition = worldPosition.xyz;
    o.Position = mul( worldPosition, ViewProjection);
    float3 N = ObjectToWorldNormal(skinnedNormal.xyz);
    float3 T = ObjectToWorldDir(skinnedTangent.xyz);
    // important for normal mapping
    T = normalize(T - N * dot(N, T));
    o.normal = N;
    o.tangent = float4(T , input.tangent.w);
    o.UV = input.UV;
    return o;
}

#ifdef DEPTHPASS

void MAIN_PS(VSOutput input)
{
    
}

#else

float4 MAIN_PS(VSOutput input, bool isFrontFace : SV_IsFrontFace) : SV_Target
{
    
    float backSideFlip = isFrontFace ? 1 : -1;
    float3 normal = normalize(input.normal) * backSideFlip;
    float3 tangent = normalize(input.tangent.xyz);
    float3 bitangent = normalize(cross(normal, tangent)) * input.tangent.w;


    float3x3 TBN =  float3x3(tangent, bitangent, normal);

    float3 normalTS = float3(0,0,1);
    
    if((flags & NormalTexFlag) != 0)
    {
        float3 texNormal = 
            TextureHeap[normalTextureIndex]
            .Sample(LinearSampler, input.UV)
            .xyz;
        normalTS = texNormal * 2 - 1;
        normalTS.xy *= NormalScale;
        normalTS.z = sqrt(1.0 - min(1, dot(normalTS.xy, normalTS.xy)));
    }

    float3 normalWS = normalize(mul(normalTS, TBN));


    
    float3 worldPos = input.worldPosition;
    float3 V = normalize(CameraPos - worldPos);
    
    float3 baseColor = albedoColor;
    if ((flags & AlbedoTexFlag) != 0)
    {
        baseColor = TextureHeap[albedoTextureIndex].Sample(LinearSampler, input.UV).xyz;
    }
    float metallic = Metallic;
    float alpha = Roughness * Roughness;
    float ambientIntensity = AmbientIntensity;

    if((flags & PackedTexFlag) != 0)
    {
        float4 packed = TextureHeap[packedTextureIndex].Sample(LinearSampler, input.UV);
        metallic = packed.b;
        alpha = packed.g;
    }

    alpha = max(alpha, 0.001);
    
    float3 F0 = lerp(float3(0.04,0.04,0.04), baseColor, metallic);

    float3 kS = FresnelSchlick(
        saturate(dot(normalWS, V)),
        F0
    );

    float3 kD = (1.0 - kS) * (1.0 - metallic);

    float3 ambient =
        kD * baseColor * ambientIntensity;
    float3 hdr = ambient;

    for (int i = 0; i < LightCount; i++)
    {
        hdr += EvaluateSpotLightPBR(
            worldPos,
            normalWS,
            V,
            baseColor, 
            alpha , metallic,
            F0,
            Lights[i]
        );
    }
    return float4(TonemapACES(hdr), 1.0f);
}

#endif