using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using static Vortice.Direct3D12.D3D12;

public class GraphicsContext : IDisposable
{

    private static ID3D12Device _device;
    private ID3D12CommandQueue _commandQueue;
    public ID3D12Device Device { get => _device; }
    public ID3D12CommandQueue CommandQueue { get => _commandQueue; }



    public GraphicsContext()
    {
#if DEBUG
        if (D3D12GetDebugInterface<ID3D12Debug1>() is ID3D12Debug1 debug)
        {
            debug.EnableDebugLayer();
            debug.SetEnableGPUBasedValidation(true);
        }

        if (D3D12GetDebugInterface<ID3D12DeviceRemovedExtendedDataSettings>()
            is ID3D12DeviceRemovedExtendedDataSettings dred)
        {
            dred.SetAutoBreadcrumbsEnablement(DredEnablement.ForcedOn);
            dred.SetPageFaultEnablement(DredEnablement.ForcedOn);
            dred.SetWatsonDumpEnablement(DredEnablement.ForcedOn);
        }
#endif
        var hr = D3D12CreateDevice(null, FeatureLevel.Level_11_0, out _device!);
        if (hr.Failure || _device == null)
            throw new NotSupportedException("Failed to create D3D12 device.");

#if DEBUG
        if (_device.QueryInterfaceOrNull<ID3D12InfoQueue>() is ID3D12InfoQueue infoQueue)
        {
            infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, false);
            infoQueue.SetBreakOnSeverity(MessageSeverity.Error, false);
            infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, false);
        }
#endif
        _commandQueue = _device!.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));

    }

    public void Dispose()
    {
        var reason = _device.DeviceRemovedReason;
        Console.WriteLine($"DeviceRemovedReason: 0x{reason.Code:X}");

        var dred = _device.QueryInterfaceOrNull<ID3D12DeviceRemovedExtendedData>();
        if (dred == null)
        {
            Console.WriteLine("DRED interface not available.");
            return;
        }

        if (dred.GetAutoBreadcrumbsOutput(out DredAutoBreadcrumbsOutput breadcrumbs).Success)
        {
            var node = breadcrumbs.HeadAutoBreadcrumbNode;
            while (node != null)
            {
                string? name = node.CommandListDebugName;
                Console.WriteLine($"Command list node: {name}");

                uint completed = (uint)(node.BreadcrumbCount > 0 && node.LastBreadcrumbValue != null
                    ? node.LastBreadcrumbValue
                    : 0);

                Console.WriteLine($"Completed breadcrumb: {completed} / {node.BreadcrumbCount}");
                node = node.Next;
            }

        }

        if (dred.GetPageFaultAllocationOutput(out DredPageFaultOutput pageFault).Success)
        {
            Console.WriteLine($"Page fault VA: 0x{pageFault.PageFaultVA:X}");

            var head = pageFault.HeadExistingAllocationNode;
            while (head != null)
            {
                string? objName = head.ObjectName;
                Console.WriteLine($"Existing allocation: {objName}");
                head = head.Next;
            }

            head = pageFault.HeadRecentFreedAllocationNode;
            while (head != null)
            {
                string? objName = head.ObjectName;
                Console.WriteLine($"Recently freed allocation: {objName}");
                head = head.Next;
            }

        }

        _commandQueue?.Dispose();
        _device?.Dispose();
    }

    public static void DumpInfoQueue()
    {
        var queue = _device.QueryInterfaceOrNull<ID3D12InfoQueue>();
        if (queue == null)
            return;

        ulong count = queue.NumStoredMessages;
        for (ulong i = 0; i < count; i++)
        {
            var message = queue.GetMessage(i);
            Console.WriteLine($"D3D12: {message.Description}");
        }

        queue.ClearStoredMessages();
    }
}