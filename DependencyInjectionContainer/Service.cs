using System.Reflection;

namespace DependencyInjectionContainer;
using Enums;

internal sealed class Service
{
    private object? serviceInstance;
    private readonly Func<DiContainer, object>? implementationFactory;

    public Service(Type interfaceType, Type implementationType, ServiceLifetime lifetime, Func<DiContainer, object> implementationFactory)
    {
        if (!interfaceType.IsAbstract)
            throw new ArgumentException("First type should be abstract");

        Key = interfaceType;
        Value = implementationType;
        Lifetime = lifetime;
        this.implementationFactory = implementationFactory;
    }
    public Service(Type interfaceType, Type implementationType, ServiceLifetime lifetime)
    {
        if (!interfaceType.IsAbstract)
            throw new ArgumentException("First type should be abstract");

        Key = interfaceType;
        Value = implementationType;
        Lifetime = lifetime;
    }

    public Service(Type implementationType, ServiceLifetime lifetime, Func<DiContainer, object> implementationFactory)
    {
        Key = Value = implementationType;
        Lifetime = lifetime;
        this.implementationFactory = implementationFactory;
    }

    public Service(Type implementationType, ServiceLifetime lifetime)
    {
        Key = Value = implementationType;
        Lifetime = lifetime;
    }

    public Service(object instance, ServiceLifetime lifetime)
    {
        Key = Value = instance.GetType();
        serviceInstance = instance;
        Lifetime = lifetime;
    }

    public Type Key { get; init; }
    public Type Value { get; init; }
    public ServiceLifetime Lifetime { get; init; }
    
    public object GetOrCreateImplementation_SaveIfSingleton(DiContainer container, ResolveStrategy resolveSource)
    {
        if (serviceInstance is not null)
        {
            return serviceInstance;
        }

        var implementation = GetCreatedImplementationForService();

        if (Lifetime == ServiceLifetime.Singleton)
        {
            serviceInstance = implementation;
            if (serviceInstance is IDisposable disposableService)
            {
                container.ServicesDisposer.Add(disposableService);
            }
        }

        return implementation;

        object GetCreatedImplementationForService()
        {
            if (implementationFactory is not null)
            {
                return implementationFactory(container);
            }

            var ctor = Value
                                    .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                    .Single();

            var parameters = ctor
                .GetParameters()
                .Select(parameter => container.Resolve(parameter.ParameterType, resolveSource))
                .ToArray();

            var createdImplementation = ctor.Invoke(parameters);
            return createdImplementation;
        }
    }
}