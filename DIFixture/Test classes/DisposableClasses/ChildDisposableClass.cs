namespace DIFixture.Test_classes.DisposableClasses;

internal sealed class ChildDisposableClass : DisposableClass
{
    public ChildDisposableClass(DisposableSequence sequence) : base(sequence) {}
}

