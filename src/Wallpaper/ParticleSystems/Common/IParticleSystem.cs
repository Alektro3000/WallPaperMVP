namespace ParticleSystems;

public interface IParticleSystem
{
    
};
public interface IParticleSystem<TSettings > : IParticleSystem  where TSettings : ISettings 
{
}