
using System.Numerics;
using Settings;

namespace Models;


public class Settings : ISettings
{
    [UiRange(0, 1, 1)]
    public float useFreeCamera;
    public Vector3 cameraPos;
    public Vector3 centerPos;

    public float AnimationSpeed;
    public float NormalScale;
    public float CameraSpeed;
    [UiRange(0, 1, 1)]
    public float showUV;
    [UiRange(0, 1, 1)]
    public float showNormal;
    [UiRange(0, 1, 1)]
    public float showShadows;
    [UiRange(0, 1, 1)]
    public float showDoubleSided;
    [UiRange(0, 1, 1)]
    public float showSingleSided;
    
    [UiRange(0, 1, 1)]
    public float loadRoom;
    [UiRange(-1, 10, 1)]
    public float showConcreteLight;
    public float metallic;
    public float roughness;
    public float ambientIntensity;
    public float Intensity;
}