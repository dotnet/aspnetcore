/* TODO: Take a temp dependency on Ms.Aspnet.Hosting until AssemblyNeutral gets fixed.
using System;
using System.Threading.Tasks;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Hosting.Server
{
    [AssemblyNeutral]
    public interface IServerFactory
    {
        // IServerConfiguration CreateConfiguration();
        // IDisposable Start(IServerConfiguration serverConfig, Func<object, Task> app);
        IDisposable Start(Func<object, Task> app);
    }
}
*/