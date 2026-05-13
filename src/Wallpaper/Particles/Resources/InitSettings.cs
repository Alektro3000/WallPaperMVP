using Settings;

namespace Particles.Settings;
public struct CommonInitSettings {
    [UiLabel("Max Particle Amount")]
    [UiRange(0f, 65536f, 1f)]
    public float MaxParticleAmount = 0;

    public CommonInitSettings(float MaxParticleAmount = 0)
    {
        this.MaxParticleAmount = MaxParticleAmount;
    }
}