using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;
using Microsoft.Net.Runtime;

namespace Microsoft.AspNet.Hosting.Server
{
    [AssemblyNeutral]
    public interface IServerFactory
    {
        IDisposable Start(Func<object, Task> application);
    }
}
