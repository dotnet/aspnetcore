using System.Runtime.Versioning;

namespace Microsoft.Net.Runtime.Services
{
    [AssemblyNeutral]
    public interface IApplicationEnvironment
    {
        string ApplicationName { get; }
        string Version { get; }
        string ApplicationBasePath { get; }
        FrameworkName TargetFramework { get; }
    }
}
