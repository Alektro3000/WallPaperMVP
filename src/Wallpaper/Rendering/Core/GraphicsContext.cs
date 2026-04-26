using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using static Vortice.Direct3D12.D3D12;

namespace Renderer.Core;

public class GraphicsContext : IDisposable
{

    public readonly ID3D12Device Device;
    public readonly ID3D12CommandQueue CommandQueue;



    public GraphicsContext()
    {
#if DEBUG
        if (D3D12GetDebugInterface(out ID3D12Debug? debug).Success)
        {
            debug!.EnableDebugLayer();

            var debug1 = debug.QueryInterfaceOrNull<ID3D12Debug1>();
            debug1?.SetEnableGPUBasedValidation(true);
            debug1?.SetEnableSynchronizedCommandQueueValidation(true);
            debug1?.Dispose();

            debug.Dispose();
        }

        if (D3D12GetDebugInterface(out ID3D12DeviceRemovedExtendedDataSettings? dred).Success)
        {
            dred!.SetAutoBreadcrumbsEnablement(DredEnablement.ForcedOn);
            dred.SetPageFaultEnablement(DredEnablement.ForcedOn);
            dred.SetWatsonDumpEnablement(DredEnablement.ForcedOn);
            dred.Dispose();
        }
#endif
        var hr = D3D12CreateDevice(null, FeatureLevel.Level_11_0, out Device!);
        if (hr.Failure || Device == null)
            throw new NotSupportedException("Failed to create D3D12 device.");

#if DEBUG
        if (Device.QueryInterfaceOrNull<ID3D12InfoQueue>() is ID3D12InfoQueue infoQueue)
        {
            infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, false);
            infoQueue.SetBreakOnSeverity(MessageSeverity.Error, false);
            infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, false);
        }
#endif
        CommandQueue = Device!.CreateCommandQueue(new CommandQueueDescription(CommandListType.Direct));

    }

    public void Dispose()
    {
        var reason = Device.DeviceRemovedReason;
        Console.WriteLine($"DeviceRemovedReason: 0x{reason.Code:X}");

        var dred = Device.QueryInterfaceOrNull<ID3D12DeviceRemovedExtendedData>();
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

        CommandQueue?.Dispose();
        Device?.Dispose();
    }

    public void DumpInfoQueue()
    {
        var queue = Device.QueryInterfaceOrNull<ID3D12InfoQueue>();
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