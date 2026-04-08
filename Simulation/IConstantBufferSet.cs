using Vortice.Direct3D12;

public interface IConstantBufferSet : IDisposable
{
    void InitBuffers(FrameResource frameResource, ID3D12Device device);
}