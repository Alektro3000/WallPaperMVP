using Vortice.Direct3D12;

public class CommonBuffers : IConstantBufferSet
{
    public FrameManager.ConstantKey commonKey;
    public CommonBuffers(FrameManager manager)
    {
        commonKey = manager.ReserveBuffer();
    }

    public void Dispose()
    {
        
    }

    public void InitBuffers(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(commonKey,BufferHelper.CreateConstantBuffer<CommonConstantBuffer>(device, "CommonBuffer"));
    }
}