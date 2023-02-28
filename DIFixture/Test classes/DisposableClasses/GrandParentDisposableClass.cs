using JetBrains.Annotations;

namespace DIFixture.Test_classes.DisposableClasses;

internal sealed class GrandParentDisposableClass : DisposableClass
{
    [UsedImplicitly]
    public GrandParentDisposableClass(ParentDisposableClass childClass, DisposableSequence sequence) : base(sequence){}
}

