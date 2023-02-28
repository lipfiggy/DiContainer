using JetBrains.Annotations;

namespace DIFixture.Test_classes.DisposableClasses;

internal sealed class ParentDisposableClass : DisposableClass
{
    [UsedImplicitly]
    public ParentDisposableClass(ChildDisposableClass childClass, DisposableSequence sequence) : base(sequence){}
}

