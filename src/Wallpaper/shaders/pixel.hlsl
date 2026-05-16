
struct PSInput
{
    float4 Position : SV_Position;
    float4 Color    : COLOR;
};


float4 MAIN_PS(PSInput input) : SV_Target
{
    return input.Color;
}