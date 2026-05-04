using System.Numerics;
using Particles.Settings;
using Renderer;
using Renderer.Core;
using Renderer.FrameManagement;

namespace Particles.Shared.Global;

public class Controller(Buffers buffers) : IConstantUpdater
{
    public float DeltaTime = -1;

    public void UpdateConstants(FrameResource currentResource, SystemSettings systemSettings)
    {
        ref var constant = ref currentResource.GetBufferConstantRef(buffers.commonKey);
        
        constant.FrameIndex = currentResource.frameMetric.FrameIndex;
        if(DeltaTime < 0)
        {
            DeltaTime = currentResource.frameMetric.DeltaTime;
        }
        constant.DeltaTime = currentResource.frameMetric.DeltaTime;
        DeltaTime = DeltaTime * 0.98f + constant.DeltaTime * 0.02f;
        constant.SmoothedDeltaTime = DeltaTime;

        constant.viewMatrix =
            Matrix4x4.Transpose(
                Matrix4x4.CreateScale((float)currentResource.frameMetric.height / currentResource.frameMetric.width, 1, 1)
                );
        constant.height = Field.Buffers.height;
        constant.width = Field.Buffers.width;
        constant.ScreenRatio = (float)currentResource.frameMetric.width/currentResource.frameMetric.height;
        constant.ScreenRatioInv = (float)currentResource.frameMetric.height/currentResource.frameMetric.width;
    }

}