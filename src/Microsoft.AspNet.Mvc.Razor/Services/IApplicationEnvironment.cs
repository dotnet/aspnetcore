using System.Runtime.Versioning;
using Microsoft.Net.Runtime.Services;

namespace Microsoft.Net.Runtime
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
