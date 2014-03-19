using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Hosting.Server
{
    // TODO: [AssemblyNeutral]
    public interface IServerFactory
    {
        IServerInformation Initialize(IConfiguration configuraiton);
        IDisposable Start(IServerInformation serverInformation, Func<object, Task> application);
    }
}
