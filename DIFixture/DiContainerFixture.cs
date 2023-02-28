using DependencyInjectionContainer;
using DependencyInjectionContainer.Enums;
using DependencyInjectionContainer.Exceptions;
using DIFixture.Test_classes.DisposableClasses;
using DIFixture.Test_classes;

namespace DIFixture;

public class DiContainerFixture
{
    private DiContainerBuilder builder = new();

    [SetUp]
    public void Setup()
    {
        builder = new DiContainerBuilder();
    }

    [Test]
    public void Resolve_ResolveServiceWithManyConstructorWhereImplementationFactoryWasDefined_ShouldResolveByFactory()
    {
        ManyConstructors manyConstructors = new ManyConstructors();
        builder.RegisterWithImplementation(manyConstructors, ServiceLifetime.Singleton);
        using var container = builder.Build();
        var resolved = container.Resolve<ManyConstructors>();
        Assert.That(resolved.ConstructorUsed, Is.EqualTo("Without parameters"));

        var child1Builder = container.CreateChildContainer();
        child1Builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Transient);
        child1Builder.Register<ManyConstructors>(ServiceLifetime.Transient,
            usedContainer =>
            {
                var logger = usedContainer.Resolve<IErrorLogger>();
                return new ManyConstructors(logger);
            });
        using (var childContainer = child1Builder.Build())
        {
            var resolvedFromChild = childContainer.Resolve<ManyConstructors>();
            Assert.That(resolvedFromChild.ConstructorUsed, Is.EqualTo("With IErrorLogger"));
        }

        var child2Builder = container.CreateChildContainer();
        child2Builder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Transient);
        child2Builder.Register<ManyConstructors>(ServiceLifetime.Transient,
            usedContainer =>
            {
                var directory = usedContainer.Resolve<IUserDirectory>();
                return new ManyConstructors(directory);
            });
        using (var childContainer = child2Builder.Build())
        {
            var resolvedFromChild = childContainer.Resolve<ManyConstructors>();
            Assert.That(resolvedFromChild.ConstructorUsed, Is.EqualTo("With IUserDirectory"));
        }
    }

    [Test]
    public void Resolve_ServiceRegisteredByInterfaceResolveByImplementationType_ShouldThrowServiceNotFoundException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<ConsoleLoggerWithAttribute>());
    }

    [Test]
    public void Resolve_ComplexGraph_ShouldReturnImplementation()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Transient);
        builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Transient);
        builder.Register<IUserFile, SystemFile>(ServiceLifetime.Transient);
        builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
        builder.Register<FileSystem>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.That(container.Resolve<FileSystem>().GetType(), Is.EqualTo(typeof(FileSystem)));
    }

    [Test]
    public void Resolve_NotAllConstructorParametersWasRegistered_ShouldThrowServiceNotFoundException()
    {
        builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Transient);
        builder.Register<FileSystem>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<FileSystem>());
    }

    [Test]
    public void Resolve_ResolveIEnumerable_ShouldReturnEnumerableOfResolvedObjectsThatImplementsType()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        var resolved = container.Resolve<IEnumerable<IErrorLogger>>().ToList();
        Assert.That(resolved.Count, Is.EqualTo(2));
        Assert.That(resolved.Count(logger => logger is ConsoleLoggerWithAttribute), Is.EqualTo(1));
        Assert.That(resolved.Count(logger => logger is FileLogger), Is.EqualTo(1));
    }

    [Test]
    public void Resolve_ResolveClassImplementsIEnumerableWhichNotRegistered_ShouldThrowServiceNotFoundException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.Throws<ServiceNotFoundException>(() => container.Resolve<List<IErrorLogger>>());
    }

    [Test]
    public void Resolve_HasManyServicesThatImplementConstructorParameterInterface_ShouldThrowResolveServiceException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        builder.Register<IUserDirectory, PublicDirectoryWithAttribute>(ServiceLifetime.Transient);
        builder.Register<IUserFile, UserFile>(ServiceLifetime.Transient);
        builder.Register<FileSystem>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.Throws<ResolveServiceException>(() => container.Resolve<FileSystem>());
    }

    [Test]
    public void Resolve_ByInterfaceWhenTwoTypesImplementsItInOneContainer_ShouldThrowResolveServiceException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.Throws<ResolveServiceException>(() => container.Resolve<IErrorLogger>());
    }

    [Test]
    public void Resolve_Local_ShouldGetTypeOnlyFromACurrentContainer()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.Local).GetType(), Is.EqualTo(typeof(FileLogger)));
    }

    [Test]
    public void Resolve_LocalWhenNotRegisteredInLocalContainerButRegisteredInParent_ShouldThrowServiceNotFoundException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        using var childContainer = parentContainer.CreateChildContainer().Build();
        Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveStrategy.Local));
    }

    [Test]
    public void Resolve_NonLocal_ShouldGetTypeOnlyFromAParentContainers()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
    }

    [Test]
    public void Resolve_NonLocalWhenNotRegisteredInParentButRegisteredInCurrentContainer_ShouldThrowServiceNotFoundException()
    {
        using var parentContainer = builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        Assert.Throws<ServiceNotFoundException>(() => childContainer.Resolve<IErrorLogger>(ResolveStrategy.NonLocal));
    }

    [Test]
    public void Resolve_NonLocalWhenDoNotHaveThisTypeInParentButHaveInAParentOfParent_ShouldGetFromParentOfParent()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var grandParentContainer = builder.Build();
        using var parentContainer = grandParentContainer.CreateChildContainer().Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        Assert.That(childContainer.Resolve<IErrorLogger>(ResolveStrategy.NonLocal).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
    }

    [Test]
    public void Resolve_NonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.Throws<NullReferenceException>(() => container.Resolve<IErrorLogger>(ResolveStrategy.NonLocal));
    }

    [Test]
    public void Resolve_AnyWhenTypeExistsInCurrentContainerAndInParent_GetFromCurrent()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        Assert.That(childContainer.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(FileLogger)));
    }

    [Test]
    public void Resolve_AnyWhenTypeExistsInParentButNotInCurrentContainer_GetFromParent()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        using var childContainer = parentContainer.CreateChildContainer().Build();
        Assert.That(childContainer.Resolve<IErrorLogger>().GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
    }

    [Test]
    public void Resolve_TypeWithValueTypeParameterInConstructor_ShouldThrowArgumentException()
    {
        builder.Register<TypeWithIntParameter>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.Throws<ArgumentException>(() => container.Resolve<TypeWithIntParameter>());
    }

    [Test]
    public void Resolve_TypeWithValueTypeParameterInConstructorRegisteredByImplementation_ShouldBeResolved()
    {
        TypeWithIntParameter typeWithIntParameter = new TypeWithIntParameter(3);
        builder.RegisterWithImplementation(typeWithIntParameter, ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.IsTrue(ReferenceEquals(typeWithIntParameter, container.Resolve<TypeWithIntParameter>()));
    }

    [Test]
    public void ResolveMany_ByInterfaceWhenTwoTypesImplementsIt_ShouldGetEnumerableOfServices()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        var resolved = container.ResolveMany<IErrorLogger>().ToList();
        Assert.That(resolved.Count, Is.EqualTo(2));
        Assert.That(resolved.Count(logger => logger is ConsoleLoggerWithAttribute), Is.EqualTo(1));
        Assert.That(resolved.Count(logger => logger is FileLogger), Is.EqualTo(1));
    }

    [Test]
    public void ResolveMany_Local_ShouldGetTypesOnlyFromACurrentContainer()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.Local).ToList();
        Assert.That(resolved.Count, Is.EqualTo(1));
        Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(FileLogger)));
    }

    [Test]
    public void ResolveMany_LocalWhenNotRegisteredInLocalContainerButRegisteredInParent_ShouldReturnEmptyIEnumerable()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        using var childContainer = parentContainer.CreateChildContainer().Build();
        Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.Local).Count(), Is.EqualTo(0));
    }

    [Test]
    public void ResolveMany_NonLocal_ShouldGetTypesOnlyFromAParentContainer()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        var resolved = childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.NonLocal).ToList();
        Assert.That(resolved.Count, Is.EqualTo(1));
        Assert.That(resolved.ElementAt(0).GetType(), Is.EqualTo(typeof(ConsoleLoggerWithAttribute)));
    }

    [Test]
    public void ResolveMany_NonLocalWhenNotRegisteredInParentButRegisteredInCurrentContainer_ShouldReturnEmptyIEnumerable()
    {
        using var parentContainer = builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        Assert.That(childContainer.ResolveMany<IErrorLogger>(ResolveStrategy.NonLocal).Count(), Is.EqualTo(0));
    }

    [Test]
    public void ResolveMany_NonLocalWhenContainerDoNotHaveParent_ShouldThrowNullRefException()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        builder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var container = builder.Build();
        Assert.Throws<NullReferenceException>(() => container.ResolveMany<IErrorLogger>(ResolveStrategy.NonLocal));
    }

    [Test]
    public void ResolveMany_AnyWhenTypeImplementsInterfaceExistsInCurrentContainerAndInParent_GetFromBoth()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, FileLogger>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        var resolved = childContainer.ResolveMany<IErrorLogger>().ToList();
        Assert.That(resolved.Count, Is.EqualTo(2));
        Assert.That(resolved.Count(logger => logger is ConsoleLoggerWithAttribute), Is.EqualTo(1));
        Assert.That(resolved.Count(logger => logger is FileLogger), Is.EqualTo(1));
    }

    [Test]
    public void ResolveMany_AnyWhenServicesOfSameTypeExistsInCurrentContainerAndInParent_GetFromChild()
    {
        builder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var parentContainer = builder.Build();
        var childBuilder = parentContainer.CreateChildContainer();
        childBuilder.Register<IErrorLogger, ConsoleLoggerWithAttribute>(ServiceLifetime.Singleton);
        using var childContainer = childBuilder.Build();
        var resolved = childContainer.ResolveMany<IErrorLogger>().ToList();
        Assert.That(resolved.Count, Is.EqualTo(1));
        Assert.IsTrue(ReferenceEquals(resolved.Single(), childContainer.Resolve<IErrorLogger>(ResolveStrategy.Local)));
        Assert.IsFalse(ReferenceEquals(resolved.Single(), parentContainer.Resolve<IErrorLogger>(ResolveStrategy.Local)));
    }

    [Test]
    public void Dispose_ChildContainerDisposed_ParentContainerShouldNotBeDisposed()
    {
        builder.Register<IUserDirectory, HiddenDirectory>(ServiceLifetime.Singleton);
        {
            var parentContainer = builder.Build();
            var childContainerBuilder = parentContainer.CreateChildContainer();
            var childContainer = childContainerBuilder.Build();
            childContainer.Dispose();
            Assert.Throws<InvalidOperationException>(() => childContainer.Dispose());
            Assert.That(parentContainer.Resolve<IUserDirectory>(), Is.Not.Null);
            parentContainer.Dispose();
        }
    }

    [Test]
    public void Dispose_ShouldBeDisposedStartingFromParentToChildClass()
    {
        builder.Register<DisposableSequence>(ServiceLifetime.Singleton);
        builder.Register<GrandParentDisposableClass>(ServiceLifetime.Singleton);
        builder.Register<ParentDisposableClass>(ServiceLifetime.Singleton);
        builder.Register<ChildDisposableClass>(ServiceLifetime.Singleton);
        var container = builder.Build();
        var disposeSequence = container.Resolve<DisposableSequence>();
        container.Resolve<GrandParentDisposableClass>();
        container.Dispose();
        List<Type> expected = new List<Type>() { typeof(GrandParentDisposableClass), typeof(ParentDisposableClass), typeof(ChildDisposableClass) };
        CollectionAssert.AreEqual(expected, disposeSequence.GetDisposedClasses());
    }

    [Test]
    public void Dispose_ResolveSingletonServiceTwice_ShouldDisposeOneTime()
    {
        builder.Register<DisposableSequence>(ServiceLifetime.Singleton);
        builder.Register<ChildDisposableClass>(ServiceLifetime.Singleton);
        var container = builder.Build();
        var disposeSequence = container.Resolve<DisposableSequence>();
        container.Resolve<ChildDisposableClass>();
        container.Resolve<ChildDisposableClass>();
        container.Dispose();
        Assert.That(disposeSequence.GetDisposedClasses().Count(), Is.EqualTo(1));
    }

    [Test]
    public void Dispose_DisposeSecondTime_ShouldThrowInvalidOperationException()
    {
        var container = builder.Build();
        container.Dispose();
        Assert.Throws<InvalidOperationException>(() => container.Dispose());
    }

    [Test]
    public void Dispose_ResolveAfterDispose_ShouldThrowInvalidOperationException()
    {
        builder.Register<ChildDisposableClass>(ServiceLifetime.Singleton);
        var container = builder.Build();
        container.Dispose();
        Assert.Throws<InvalidOperationException>(() => container.Resolve<ChildDisposableClass>());
    }
}