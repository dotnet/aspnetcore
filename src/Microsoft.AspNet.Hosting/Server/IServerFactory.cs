using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Hosting.Server
{
    public interface IServerFactory
    {
        IDisposable Start(Func<object, Task> application);
    }
}
