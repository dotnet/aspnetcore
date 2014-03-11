using System.Runtime.Versioning;

namespace Microsoft.Net.Runtime
{
    [AssemblyNeutral]
    public interface ILibraryExportProvider
    {
        ILibraryExport GetLibraryExport(string name, FrameworkName targetFramework);
    }
}
