using JetBrains.Annotations;

namespace DependencyInjectionContainer;
using Attributes;
using Enums;
using Exceptions;
using System.Reflection;

public sealed class DiContainerBuilder
{
    private readonly List<Service> services = new();
    private readonly DiContainer? parentContainer;
    private bool isBuild;

    public DiContainerBuilder() { }
    internal DiContainerBuilder(DiContainer parent) => parentContainer = parent;

    public void Register<TImplementationInterface, TImplementation> (ServiceLifetime lifetime) where TImplementation : TImplementationInterface
    {
        CheckRegistration(lifetime, typeof(TImplementation))
            .ThrowIfManyConstructors(typeof(TImplementation))
            .services.Add(new Service(typeof(TImplementationInterface), typeof(TImplementation), lifetime));
    }

    [UsedImplicitly]
    public void Register<TImplementationInterface, TImplementation>
        (ServiceLifetime lifetime, Func<DiContainer, TImplementation> implementationFactory) where TImplementation : TImplementationInterface
    {
        CheckRegistration(lifetime, typeof(TImplementation))
            .services.Add(new Service(typeof(TImplementationInterface), typeof(TImplementation), lifetime,
                container => implementationFactory(container)!));
    }

    public void Register<TImplementation> (ServiceLifetime lifetime) where TImplementation : class
    {
        if (typeof(TImplementation).IsAbstract)
        {
            throw new RegistrationServiceException("Can't register type without assigned implementation type");
        }
        CheckRegistration(lifetime, typeof(TImplementation))
            .ThrowIfManyConstructors(typeof(TImplementation))
            .services.Add(new Service(typeof(TImplementation), lifetime));
    }

    public void Register<TImplementation>
        (ServiceLifetime lifetime, Func<DiContainer, TImplementation> implementationFactory) where TImplementation : class
    {
        if (typeof(TImplementation).IsAbstract)
        {
            throw new RegistrationServiceException("Can't register type without assigned implementation type");
        }

        CheckRegistration(lifetime, typeof(TImplementation))
            .services.Add(new Service(typeof(TImplementation), lifetime, implementationFactory));
    }

    public void RegisterWithImplementation(object implementation, ServiceLifetime lifetime)
    {
        CheckRegistration(lifetime, implementation.GetType())
            .services.Add(new Service(implementation, lifetime));
    }

    public void RegisterAssemblyByAttributes(Assembly assembly)
    {
        ThrowIfContainerBuilt();

        var typesWithRegisterAttribute = assembly
                                         .GetTypes()
                                         .Where(t => t.GetCustomAttribute<RegisterAttribute>() != null);

        foreach (var type in typesWithRegisterAttribute)
        {
            var serviceInfo = type.GetCustomAttribute<RegisterAttribute>()!;

                ThrowIfTransientDisposable(serviceInfo.Lifetime, type)
                .ThrowIfImplementationTypeInappropriate(type)
                .services.Add(serviceInfo.IsRegisteredByInterface
                          ? new Service(serviceInfo.InterfaceType!, type, serviceInfo.Lifetime)
                          : new Service(type, serviceInfo.Lifetime));
        }
    }

    public DiContainer Build()
    {
        if (isBuild)
        {
            throw new InvalidOperationException("Container was built already");
        }
        isBuild = true;
        return new DiContainer(services, parentContainer);
    }

    private DiContainerBuilder CheckRegistration(ServiceLifetime lifetime, Type implementationType)
    {
        return ThrowIfContainerBuilt()
               .ThrowIfTransientDisposable(lifetime, implementationType)
               .ThrowIfImplementationTypeInappropriate(implementationType);
    }

    private DiContainerBuilder ThrowIfContainerBuilt()
    {
        if (isBuild)
        {
            throw new RegistrationServiceException("This container was built already");
        }
        return this;
    }

    [AssertionMethod]
    private DiContainerBuilder ThrowIfTransientDisposable(ServiceLifetime lifetime, Type implementationType)
    {
        if(lifetime == ServiceLifetime.Transient && implementationType.GetInterface(nameof(IDisposable)) != null)
        {
            throw new RegistrationServiceException("It is prohibited to register transient disposable service");
        }
        return this;
    }

    private DiContainerBuilder ThrowIfImplementationTypeInappropriate(Type implementationType)
    {
        if (services.Any(service => service.Value == implementationType))
        {
            throw new RegistrationServiceException($"Service with type {implementationType.FullName} has been already registered");
        }
        return this;
    }


    [AssertionMethod]
    private DiContainerBuilder ThrowIfManyConstructors(Type implementationType)
    {
        if (implementationType.GetConstructors().Length != 1)
        {
            throw new RegistrationServiceException(
                "It is prohibited to register types with many constructors. Try to define ctor or select type with one ctor");
        }

        return this;
    }
}
