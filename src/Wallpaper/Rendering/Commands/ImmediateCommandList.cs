
using Renderer.Core;
using Vortice.Direct3D12;

namespace Renderer.Commands;

public class ImmediateCommandList : IDisposable 
{
    private readonly ID3D12CommandAllocator CommandAllocator;
    private readonly ID3D12GraphicsCommandList CommandList;
    private readonly ID3D12CommandQueue CommandQueue;
    
    private readonly AutoResetEvent FenceEvent;
    private ulong FenceValue;
    private readonly ID3D12Fence Fence;

    public ImmediateCommandList(GraphicsContext context) : this(context.Device, context.CommandQueue){}
    public ImmediateCommandList(ID3D12Device device, ID3D12CommandQueue commandQueue)
    {
        CommandAllocator =
            device.CreateCommandAllocator(CommandListType.Direct);

        CommandList =
            device.CreateCommandList<ID3D12GraphicsCommandList>(
                0,
                CommandListType.Direct,
                CommandAllocator,
                null);
        CommandList.Close(); // put it into a resettable state

        CommandQueue = commandQueue;

        FenceValue = 1;
        Fence = device.CreateFence(0);
        FenceEvent = new AutoResetEvent(false);
    }

    public void Dispose()
    {
        FenceEvent.Dispose();
        Fence.Dispose();
        CommandList.Dispose();
        CommandAllocator.Dispose();
    }

    public void ExecuteImmediate(Action<ID3D12GraphicsCommandList> record)
    {
        CommandAllocator.Reset();
        CommandList.Reset(CommandAllocator, null);

        record(CommandList);

        CommandList.Close();
        CommandQueue.ExecuteCommandList(CommandList);
        WaitForFence();
    }

    private void WaitForFence()
    {
        ulong fenceValue = FenceValue;
        CommandQueue.Signal(Fence, fenceValue);
        FenceValue++;

        if (Fence.CompletedValue < fenceValue)
        {
            Fence.SetEventOnCompletion(
                fenceValue,
                FenceEvent.SafeWaitHandle.DangerousGetHandle());

            FenceEvent.WaitOne();
        }
    }
}