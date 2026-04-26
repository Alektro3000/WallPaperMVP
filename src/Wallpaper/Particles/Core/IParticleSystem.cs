using Particles.Settings;

namespace Particles.Core;

public interface IParticleSystem
{
    
};
public interface IParticleSystem<TSettings > : IParticleSystem  where TSettings : ISettings 
{
}