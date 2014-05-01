using System;
using System.Runtime.Versioning;

namespace Microsoft.Net.Runtime
{
    /// <summary>
    /// Service provided by the host containing application environment details.
    /// </summary>
    [AssemblyNeutral]
    public interface IApplicationEnvironment
    {
        string ApplicationName { get; }
        string Version { get; }
        string ApplicationBasePath { get; }
        FrameworkName TargetFramework { get; }
    }
}