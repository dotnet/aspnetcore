using System.Runtime.Versioning;

namespace Microsoft.Net.Runtime
{
    [AssemblyNeutral]
    public interface IDependencyExporter
    {
        IDependencyExport GetDependencyExport(string name, FrameworkName targetFramework);
    }
}