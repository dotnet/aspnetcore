using System;
using System.Threading.Tasks;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Hosting.Server
{
    [AssemblyNeutral]
    public interface IServerFactory
    {
        IServerConfiguration CreateConfiguration();
        IDisposable Start(IServerConfiguration serverConfig, Func<object, Task> app);
    }
}
