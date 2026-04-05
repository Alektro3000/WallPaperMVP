
using Vortice.Direct3D12;

public class WhirlSytem : ParticleSystem
{
    public WhirlSytem(
        ID3D12Device device, 
        ImmediateCommandList commandList, 
        GeometryBuffers GeometryBuffers, 
        HeapAllocator HeapAllocator, 
        FrameManager FrameManager, int width, int height)
    {
        ParticleBuffers = new ParticleBuffers(device, commandList, HeapAllocator);
        GraphicPass = new GraphicPass(device, ParticleBuffers, GeometryBuffers, "vertex.hlsl", "pixel.hlsl");
        ComputePass = new ComputePass(device, ParticleBuffers, "compute.hlsl", "precompute.hlsl");
        ParticleSystemController = new ParticleController(ParticleBuffers, width, height);
        ConstantKey = FrameManager.ReserveBuffer();
    }
}