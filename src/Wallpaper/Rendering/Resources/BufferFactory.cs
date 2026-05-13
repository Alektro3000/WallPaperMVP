using System;
using System.Drawing;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Renderer.Commands;
using Renderer.Shaders;
using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Renderer.Resources;

public static class BufferFactory
{
    public static UnorderedAccessViewDescription CreateStructuredBufferUavDesc<T>(uint numElements)
        where T : unmanaged
    {
        return new UnorderedAccessViewDescription
        {
            ViewDimension = UnorderedAccessViewDimension.Buffer,
            Format = Format.Unknown,
            Buffer = new BufferUnorderedAccessView
            {
                FirstElement = 0,
                NumElements = numElements,
                StructureByteStride = (uint)Marshal.SizeOf<T>(),
                CounterOffsetInBytes = 0,
                Flags = BufferUnorderedAccessViewFlags.None
            }
        };
    }
    public static ShaderResourceViewDescription CreateStructuredBufferSrvDesc<T>(uint numElements)
        where T : unmanaged
    {
        return new ShaderResourceViewDescription
        {
            ViewDimension = ShaderResourceViewDimension.Buffer,
            Shader4ComponentMapping = ShaderConstants.Shader4ComponentMapping,
            Format = Format.Unknown,
            Buffer = new BufferShaderResourceView
            {
                FirstElement = 0,
                NumElements = numElements,
                StructureByteStride = (uint)Marshal.SizeOf<T>(),
                Flags = BufferShaderResourceViewFlags.None
            }
        };

    }
    
    public static ID3D12Resource CreateUploadBuffer<T>(
        ID3D12Device device,
        ReadOnlySpan<T> data)
        where T : unmanaged
    {
        if (data.Length == 0)
            throw new ArgumentException("Data must not be empty.", nameof(data));

        uint sizeInBytes = (uint)(data.Length * (uint)Marshal.SizeOf<T>());

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

    public unsafe static ConstantBinding CreateConstantBuffer<T>(ID3D12Device device, String Name) where T : unmanaged
    {
        var constant = CreateStaticBuffer<T>(device, out var mappedConstants);
        constant.Name = Name;
        return new ConstantBinding()
        {
            ConstantBuffer = constant,
            MappedConstants = (byte*)mappedConstants,
        };
    }

    public unsafe static ID3D12Resource CreateStaticBuffer<T>(
        ID3D12Device device, out T* mappedConstants) where T : unmanaged
    {
        //constant Buffer size must be aligned to 256
        int constantBufferSize = (Marshal.SizeOf<T>() + 255) & ~255;

        ID3D12Resource _constantBuffer = CreateUploadBuffer(device, (ulong)constantBufferSize);

        void* _mappedConstants;
        _constantBuffer.Map(0, null, &_mappedConstants).CheckError();
        mappedConstants = (T*)_mappedConstants;


        return _constantBuffer;
    }

    public static ID3D12Resource CreateDefaultBuffer<T>(
    ID3D12Device device,
    uint elementCount,
        ResourceStates finalState = ResourceStates.VertexAndConstantBuffer,
        ResourceFlags resourceFlags = ResourceFlags.None)
    where T : unmanaged
    {
        ulong sizeInBytes = (ulong)Marshal.SizeOf<T>() * elementCount;

        return device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer(sizeInBytes, resourceFlags),
            finalState);
    }

    public static ID3D12Resource CreateDefaultBuffer<T>(
        ID3D12Device device,
        ReadOnlySpan<T> data,
        ImmediateCommandList commandList,
        ResourceStates finalState = ResourceStates.VertexAndConstantBuffer,
                ResourceFlags resourceFlags = ResourceFlags.None)
        where T : unmanaged
    {
        using var uploadBuffer = CreateUploadBuffer(device, data);

        uint sizeInBytes = (uint)(data.Length * (uint)Marshal.SizeOf<T>());

        ID3D12Resource buffer = device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer(sizeInBytes, resourceFlags),
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

    public static VertexBufferView CreateVertexBufferView<T>(
        ID3D12Resource buffer,
        uint elementCount, 
        uint offset = 0 )
        where T : unmanaged
    {
        uint stride = (uint)Marshal.SizeOf<T>();
        uint sizeInBytes = (uint)(elementCount * stride);

        return new VertexBufferView(
            buffer.GPUVirtualAddress + offset,
            sizeInBytes,
            stride);
    }
    public static IndexBufferView CreateIndexBufferView(
        ID3D12Resource buffer, uint elementCount, uint offset = 0 )
    {

        uint sizeInBytes = elementCount * sizeof(ushort);

        return new IndexBufferView(
            buffer.GPUVirtualAddress + offset,
            sizeInBytes);
    }

}