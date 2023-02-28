using JetBrains.Annotations;

namespace DIFixture.Test_classes;

internal sealed class ManyConstructors
{
    public ManyConstructors() => ConstructorUsed = "Without parameters";

    [UsedImplicitly]
    public ManyConstructors(IErrorLogger errorLogger) 
    {
        ConstructorUsed = "With IErrorLogger";
    }

    [UsedImplicitly]
    public ManyConstructors(IUserDirectory userDirectory)
    {
        ConstructorUsed = "With IUserDirectory";
    }

    public string ConstructorUsed { get; init; }
}
