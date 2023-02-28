using JetBrains.Annotations;

namespace DIFixture.Test_classes;

internal sealed class FileSystem
{
    [UsedImplicitly]
    public FileSystem(IEnumerable<IUserFile> files, IEnumerable<IUserDirectory> directories,
        IErrorLogger errorNotificator) {}
}
