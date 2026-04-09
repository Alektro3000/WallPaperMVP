
using Vortice.Direct3D12;

public class ImmediateCommandList : IDisposable 
{
    private ID3D12CommandAllocator _commandAllocator;
    private ID3D12GraphicsCommandList _commandList;
    private ID3D12Fence _fence;
    private ID3D12CommandQueue _commandQueue;
    private AutoResetEvent _fenceEvent;
    private ulong _fenceValue;
    public ImmediateCommandList(GraphicsContext context) : this(context.Device, context.CommandQueue){}
    public ImmediateCommandList(ID3D12Device device, ID3D12CommandQueue commandQueue)
    {
        _commandAllocator =
            device.CreateCommandAllocator(CommandListType.Direct);

        _commandList =
            device.CreateCommandList<ID3D12GraphicsCommandList>(
                0,
                CommandListType.Direct,
                _commandAllocator,
                null);
        _commandList.Close(); // put it into a resettable state

        _commandQueue = commandQueue;

        _fenceValue = 1;
        _fence = device.CreateFence(0);
        _fenceEvent = new AutoResetEvent(false);
    }

    public void Dispose()
    {
        _fenceEvent.Dispose();
        _fence.Dispose();
        _commandList.Dispose();
        _commandAllocator.Dispose();
    }

    public void ExecuteImmediate(Action<ID3D12GraphicsCommandList> record)
    {
        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator, null);

        record(_commandList);

        _commandList.Close();
        _commandQueue.ExecuteCommandList(_commandList);
        WaitForFence();
    }

    private void WaitForFence()
    {
        ulong fenceValue = _fenceValue;
        _commandQueue.Signal(_fence, fenceValue);
        _fenceValue++;

        if (_fence.CompletedValue < fenceValue)
        {
            _fence.SetEventOnCompletion(
                fenceValue,
                _fenceEvent.SafeWaitHandle.DangerousGetHandle());

            _fenceEvent.WaitOne();
        }
    }
}