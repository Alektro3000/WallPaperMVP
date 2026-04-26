

using System.Reflection;
using Particles.Settings;

namespace Particles.Core;
public static class ParticleSystemReflection
{
    public static Type GetSettingsType(Type systemType)
    {
        return systemType
            .GetInterfaces()
            .Where(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IParticleSystemFor<>))
            .Select(i => i.GetGenericArguments().First())
            .First();
    }
    public static IEnumerable<Type> GetParticleSystems()
    {
        return Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t =>
                !t.IsAbstract &&
                typeof(IParticleSystem).IsAssignableFrom(t));
    }

    public static Dictionary<Type, object> GetParticleSystemsSettings()
    {
        return GetParticleSystems()
            .Select(GetSettingsType)
            .Where(t => t != null)
            .Distinct()
            .ToDictionary(t => t!, t => Activator.CreateInstance(t!))!;
    }
    static MethodInfo GetBuilderMethod(Type systemType, Type settingsType)
    {
        var method = systemType
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .FirstOrDefault(m =>
            {
                if (m.GetCustomAttribute<SystemBuilderAttribute>() == null)
                    return false;

                var parameters = m.GetParameters();

                return parameters.Length == 2 &&
                    parameters[0].ParameterType == typeof(ParticleSystemInitContext) &&
                    parameters[1].ParameterType == settingsType;
            });

        return method ?? throw new InvalidOperationException(
            $"Valid builder not found: {systemType.FullName}");
    }

    public static IEnumerable<IParticleSystem> CreateParticleSystems(SystemSettings settings, ParticleSystemInitContext context)
    {
        return GetParticleSystems()
            .Select(t => GetBuilderMethod(t, GetSettingsType(t))
                            ?.Invoke(null, [context, settings.GetSettings(GetSettingsType(t))]))
            .Where(t => t is not null)
            .Select(t => t!)
            .OfType<IParticleSystem>();
    }
}