namespace DIFixture.Test_classes.DisposableClasses;

internal abstract class DisposableClass : IDisposable
{
    private readonly DisposableSequence disposableSequence;

    protected DisposableClass(DisposableSequence sequence)
    {
        disposableSequence = sequence;
    }
    public bool IsDisposed { get; protected set; }
    public void Dispose()
    {
        if (!IsDisposed)
        {
            disposableSequence.SaveDisposedClassType(GetType());
        }
        IsDisposed = true;
    }
}

