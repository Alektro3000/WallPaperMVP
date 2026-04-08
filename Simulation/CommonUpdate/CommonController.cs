using System.Numerics;
using Vortice.Direct3D12;

public class CommonUpdater(CommonBuffers buffers) : IConstantUpdater
{
    public void UpdateConstants(FrameResource currentResource)
    {
        UpdateConstant(currentResource, ref currentResource.GetBufferConstantRef<CommonConstantBuffer>(buffers.commonKey));
    }

    private void UpdateConstant(FrameResource currentResource, ref CommonConstantBuffer constant)
    {
        constant.FrameIndex = currentResource.frameMetric.FrameIndex;
        constant.DeltaTime = currentResource.frameMetric.DeltaTime;
        constant.viewMatrix =
            Matrix4x4.Transpose(
                Matrix4x4.CreateScale((float)currentResource.frameMetric.height / currentResource.frameMetric.width, 1, 1)
                );
        constant.height = FieldBuffers.height;
        constant.width = FieldBuffers.width;
        constant.ScreenRatio = (float)currentResource.frameMetric.height / currentResource.frameMetric.width;
    }
}