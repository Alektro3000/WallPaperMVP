
using Renderer.FrameManagement;
using Vortice.Direct3D12;

public class FrameCommandList : IDisposable
{


    // Synchronization
    private readonly ID3D12Fence fence;
    private ulong fenceValue;
    private readonly AutoResetEvent fenceEvent = new(false);
    public FrameCommandList(ID3D12Device device)
    {
        fence = device.CreateFence(0);
        fenceValue = 1;
    }
        
    public void WaitForFrame(FrameResource frame)
    {
        if (frame.FenceValue != 0 && fence.CompletedValue < frame.FenceValue)
        {
            fence.SetEventOnCompletion(
                frame.FenceValue,
                fenceEvent.SafeWaitHandle.DangerousGetHandle());

            fenceEvent.WaitOne();
        }
    }
    
    public void WaitForAllFrames(FrameResource[] frameResources)
    {
        for (int i = 0; i < frameResources.Length; i++)
            WaitForFrame(frameResources[i]);
    }

    public void ResetCmd(FrameResource currentResource)
    {
        currentResource.CommandAllocator.Reset();

        var cmd = currentResource.CommandList;
        cmd.Reset(currentResource.CommandAllocator);
    }

    internal void SetSignal(FrameResource currentResource, ID3D12CommandQueue queue)
    {
        ulong fenceValue = this.fenceValue;
        queue.Signal(fence, fenceValue);
        currentResource.FenceValue = fenceValue;
        this.fenceValue++;
    }
    public void Dispose()
    {
        
        fence?.Dispose();
        fenceEvent?.Dispose();
    }
}