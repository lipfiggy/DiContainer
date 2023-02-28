using DIFixture.Test_classes.DisposableClasses;

namespace DIFixture;
using DependencyInjectionContainer;
using Test_classes;
using DependencyInjectionContainer.Exceptions;
using DependencyInjectionContainer.Enums;

public class DiContainerBuilderFixture
{
    private DiContainerBuilder builder = new();

    [SetUp]
    public void Setup()
    {
        builder = new DiContainerBuilder();
    }

    [Test]
    public void Register_TwoEqualImplementationTypesInContainer_ShouldThrowRegistrationServiceException()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => builder.Register<FileLogger>(ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient));
        var obj = new FileLogger();
        Assert.Throws<RegistrationServiceException>(() => builder.RegisterWithImplementation(obj, ServiceLifetime.Singleton));
    }

    [Test]
    public void Register_ByInterfaceOnly_ShouldThrowRegistrationServiceException()
    {
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger>(ServiceLifetime.Singleton));
    }

    [Test]
    public void RegisterWithImplementation_ShouldResolveByImplementationType()
    {
        IErrorLogger logger = new FileLogger();
        builder.RegisterWithImplementation(logger, ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.That((IErrorLogger)container.Resolve<FileLogger>(), Is.EqualTo(logger));
    }

    [Test]
    public void RegisterWithImplementation_ResolveByInterfaceType_ShouldThrowServiceNotFoundException()
    {
        IErrorLogger logger = new FileLogger();
        builder.RegisterWithImplementation(logger, ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IErrorLogger>());
    }

    [Test]
    public void Register_RegisterAfterBuild_ShouldThrowRegistrationServiceException()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using var container = builder.Build();
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => builder.Register<ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton));
        Assert.Throws<RegistrationServiceException>(() => builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Transient));
        var obj = new ConsoleLoggerWithAttribute();
        Assert.Throws<RegistrationServiceException>(() => builder.RegisterWithImplementation(obj, ServiceLifetime.Singleton));
    }

    [Test]
    public void Register_RegisterTypeWithManyConstructorsNotDefineWhichToUse_ShouldThrowRegistrationServiceException()
    {
        Assert.Throws<RegistrationServiceException>( () => builder.Register<ManyConstructors>(ServiceLifetime.Singleton));
    }

    [Test]
    public void Build_TheSecondBuild_ShouldThrowInvalidOperationException()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using var container = builder.Build();
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Test]
    public void RegisterByAssembly_ShouldGetOnlyTypesWithRegisterAttributeWhenResolve()
    {
        builder.RegisterAssemblyByAttributes(typeof(FileLogger).Assembly);
        using var container = builder.Build();
        Assert.That(container.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
        Assert.That(container.Resolve<IUserDirectory>().GetType(), Is.EqualTo(typeof(PublicDirectoryWithAttribute)));
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<IUserFile>());
    }

    [Test]
    public void Register_RegisterTypeAsSingleton_ReturnsTheSameObjectForEveryResolve()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        var obj1 = container.Resolve<IErrorLogger>();
        var obj2 = container.Resolve<IErrorLogger>();
        Assert.IsTrue(ReferenceEquals(obj1, obj2));
    }

    [Test]
    public void Register_RegisterTypeAsTransient_ReturnsNewObjectForEveryResolve()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using var container = builder.Build();
        var obj1 = container.Resolve<IErrorLogger>();
        var obj2 = container.Resolve<IErrorLogger>();
        Assert.IsFalse(ReferenceEquals(obj1, obj2));
    }

    [Test]
    public void Register_RegisterImplementationTypeInAChildContainerWhenItExistsInParent_ShouldOverrideParentsRegistration()
    {
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        var childBuilder = container.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Transient);
        using var childContainer = childBuilder.Build();
        Assert.IsFalse(ReferenceEquals(container.Resolve<IErrorLogger>(), childContainer.Resolve<IErrorLogger>()));
    }

    [Test]
    public void Register_RegisterTransientDisposable_ThrowsRegistrationServiceException()
    {
        Assert.Throws<RegistrationServiceException>(() => builder.Register<ChildDisposableClass>(ServiceLifetime.Transient));
    }

}