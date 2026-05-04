using System.Numerics;
using Particles.Settings;
using Renderer;
using Renderer.Core;
using Renderer.FrameManagement;

namespace Particles.Shared.Global;

public class Controller(Buffers buffers) : IConstantUpdater
{
    public void UpdateConstants(FrameResource currentResource, SystemSettings systemSettings)
    {
        UpdateConstant(currentResource, ref currentResource.GetBufferConstantRef(buffers.commonKey));
    }

    private void UpdateConstant(FrameResource currentResource, ref ConstantBuffer constant)
    {
        constant.FrameIndex = currentResource.frameMetric.FrameIndex;
        constant.DeltaTime = currentResource.frameMetric.DeltaTime;
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