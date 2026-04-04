using System;
using System.Drawing;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

public static class BufferHelper
{
    public static unsafe ID3D12Resource CreateUploadBuffer<T>(
        ID3D12Device device,
        ReadOnlySpan<T> data)
        where T : unmanaged
    {
        if (data.Length == 0)
            throw new ArgumentException("Data must not be empty.", nameof(data));

        uint sizeInBytes = (uint)(data.Length * sizeof(T));

        return CreateUploadBuffer(device, data, sizeInBytes);
    }
    public static unsafe ID3D12Resource CreateUploadBuffer<T>(
        ID3D12Device device,
        ReadOnlySpan<T> data,
        ulong sizeInBytes)
        where T : unmanaged
    {
        uint actualSize = (uint)(data.Length * sizeof(T));

        ID3D12Resource buffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Upload),
            HeapFlags.None,
            ResourceDescription.Buffer(sizeInBytes),
            ResourceStates.GenericRead);

        //Copy data to buffer
        void* mapped = null;
        buffer.Map(0, null, &mapped).CheckError();

        try
        {
            fixed (T* src = data)
            {
                Buffer.MemoryCopy(src, mapped, actualSize, actualSize);
            }
        }
        finally
        {
            buffer.Unmap(0);
        }

        return buffer;
    }


    public static ID3D12Resource CreateUploadBuffer(
        ID3D12Device device,
        ulong sizeInBytes)
    {
        ID3D12Resource buffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Upload),
            HeapFlags.None,
            ResourceDescription.Buffer(sizeInBytes),
            ResourceStates.GenericRead);

        return buffer;
    }

    public unsafe static ID3D12Resource CreateStaticBuffer<T>(
        ID3D12Device device, CpuDescriptorHandle cbvHandle, out T* mappedConstants)  where T : unmanaged
    {
        //constant Buffer size must be aligned to 256
        int constantBufferSize = (Marshal.SizeOf<T>() + 255) & ~255;

        ID3D12Resource _constantBuffer = CreateUploadBuffer(device, (ulong)constantBufferSize);
        
        void* _mappedConstants;
        _constantBuffer.Map(0, null, &_mappedConstants).CheckError();
        mappedConstants = (T*)_mappedConstants; 
        

            var cbvDesc = new ConstantBufferViewDescription
            {
                BufferLocation = _constantBuffer.GPUVirtualAddress,
                SizeInBytes = (uint)constantBufferSize
            };

            device.CreateConstantBufferView(
                cbvDesc,
                cbvHandle);

        return _constantBuffer;
    }


    public static unsafe ID3D12Resource CreateDefaultBuffer<T>(
        ID3D12Device device,
        ReadOnlySpan<T> data,
        ImmediateCommandList commandList,
        ResourceStates finalState = ResourceStates.VertexAndConstantBuffer,
                ResourceFlags resourceFlags = ResourceFlags.None)
        where T : unmanaged
    {
        using var uploadBuffer = CreateUploadBuffer(device, data);

        uint sizeInBytes = (uint)(data.Length * sizeof(T));

        ID3D12Resource buffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer(sizeInBytes,resourceFlags),
            ResourceStates.CopyDest);
        
        commandList.ExecuteImmediate(list =>
        {
            list.CopyBufferRegion(
                buffer, 0,
                uploadBuffer, 0,
                sizeInBytes);

            list.ResourceBarrierTransition(
                buffer,
                ResourceStates.CopyDest,
                finalState);
        });


        return buffer;
    }

    public unsafe static VertexBufferView CreateVertexBufferView<T>(
        ID3D12Resource buffer,
        uint elementCount)
        where T : unmanaged
    {
        uint stride = (uint)sizeof(T);
        uint sizeInBytes = (uint)(elementCount * (uint)sizeof(T));

        return new VertexBufferView(
            buffer.GPUVirtualAddress,
            sizeInBytes,
            stride);
    }
    public static IndexBufferView CreateIndexBufferView(
        ID3D12Resource buffer, uint elementCount)
        {
        
        uint sizeInBytes = (uint)(elementCount * sizeof(ushort));

        return new IndexBufferView(
            buffer.GPUVirtualAddress,
            sizeInBytes);
    }
}