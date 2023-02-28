namespace DIFixture.Test_classes.DisposableClasses;

internal sealed class DisposableSequence
{
    private readonly List<Type> disposedItems = new();

    public void SaveDisposedClassType(Type disposedType) => disposedItems.Add(disposedType);
    public IEnumerable<Type> GetDisposedClasses() => disposedItems;
}

