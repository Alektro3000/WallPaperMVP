
using System.ComponentModel;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Renderer;
using Renderer.Core;
using Renderer.Descriptors;
using Renderer.FrameManagement;
using Renderer.Resources;
using Renderer.Shaders;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Particles.Shared.Field;

public class Buffers : IConstantBufferSet
{
    public ID3D12Resource FieldResource;
    public GpuDescriptorHandle UAVFieldDescriptor;
    public GpuDescriptorHandle SRVFieldDescriptor;    
    public FrameManager.ConstantKey fieldKey;
    public const int width = 192;
    public const int height = 108;
    public Buffers(ID3D12Device device, FrameManager manager, HeapAllocator heap)
    {
        fieldKey = manager.ReserveBuffer();

        //Creating Buffer
        var textureDesc = ResourceDescription.Texture2D(
            Format.R32G32B32A32_Float,
            width,
            height,
            1, // arraySize
            1, // mipLevels
            1, // sampleCount
            0, // sampleQuality
            ResourceFlags.AllowUnorderedAccess
        );

        FieldResource = device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            textureDesc,
            ResourceStates.AllShaderResource
        );
        FieldResource.Name = "Field_Texture";

        //Creating Handler Uav
        (var uavDescriptorHandle, UAVFieldDescriptor) = heap.Allocate()[0];
        var uavDesc = new UnorderedAccessViewDescription
        {
            Format = Format.R32G32B32A32_Float,
            ViewDimension = UnorderedAccessViewDimension.Texture2D,
            Texture2D = new Texture2DUnorderedAccessView()
        };
        
        device.CreateUnorderedAccessView(
            FieldResource,
            null,
            uavDesc,
            uavDescriptorHandle
        );

        //Creating Handler SRV
        (var srcDescriptorHandle , SRVFieldDescriptor) = heap.Allocate()[0];
        var srvDesc = new ShaderResourceViewDescription
        {
            Format = Format.R32G32B32A32_Float,
            Shader4ComponentMapping = ShaderConstants.Shader4ComponentMapping,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new Texture2DShaderResourceView()
            {
                MostDetailedMip = 0,
                MipLevels = 1,
                PlaneSlice = 0,
                ResourceMinLODClamp = 0
            }
        };
        
        device.CreateShaderResourceView(
            FieldResource,
            srvDesc,
            srcDescriptorHandle
        );
    }

    public void InitBuffers(FrameResource frameResource, ID3D12Device device)
    {
        frameResource.AddBuffer(fieldKey,BufferFactory.CreateConstantBuffer<FieldConstantBuffer>(device, "Field Constants"));
    }

    public void Dispose()
    {
        FieldResource?.Dispose();
    }
}