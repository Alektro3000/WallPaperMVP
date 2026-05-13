using System.Numerics;
using Particles.Settings;
using Renderer;
using Renderer.Core;
using Renderer.FrameManagement;

namespace Particles.Shared.Global;

public class Controller(Buffers buffers) : IConstantUpdater
{

    public void UpdateConstants(FrameResource currentResource)
    {
        ref var constant = ref currentResource.GetBufferConstantRef(buffers.commonKey);
        
        constant.FrameIndex = currentResource.FrameMetric.FrameIndex;
        constant.DeltaTime = currentResource.FrameMetric.DeltaTime;

        constant.viewMatrix =
            Matrix4x4.Transpose(
                Matrix4x4.CreateScale((float)currentResource.FrameMetric.height / currentResource.FrameMetric.width, 1, 1)
                );
        constant.height = Field.Buffers.height;
        constant.width = Field.Buffers.width;
        constant.ScreenRatio = (float)currentResource.FrameMetric.width/currentResource.FrameMetric.height;
        constant.ScreenRatioInv = (float)currentResource.FrameMetric.height/currentResource.FrameMetric.width;
    }

}