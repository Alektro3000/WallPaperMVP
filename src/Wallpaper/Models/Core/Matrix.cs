

using System.Numerics;

public static class Matrix4x4Ex
{
    public static Matrix4x4 CreatePerspectiveFieldOfViewReversedZ(
        float fieldOfView,
        float aspectRatio,
        float nearPlaneDistance,
        float farPlaneDistance)
    {
        float height = 1.0f / float.Tan(fieldOfView * 0.5f);
        float width = height / aspectRatio;

        float range = nearPlaneDistance / (farPlaneDistance - nearPlaneDistance);

        Matrix4x4 projection = new(
                width, 0, 0, 0,
                0, height, 0, 0,
                0, 0, range, -1.0f,
                0, 0, range * farPlaneDistance, 0
                );

        return projection;
    }
}