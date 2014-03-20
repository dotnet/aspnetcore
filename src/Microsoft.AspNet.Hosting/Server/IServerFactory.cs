using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;

namespace Microsoft.AspNet.Hosting.Server
{
    // TODO: [AssemblyNeutral]
    public interface IServerFactory
    {
        IServerInformation Initialize(IConfiguration configuration);
        IDisposable Start(IServerInformation serverInformation, Func<object, Task> application);
    }
}
