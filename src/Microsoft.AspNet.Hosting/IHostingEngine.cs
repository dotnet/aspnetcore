using System;

namespace Microsoft.AspNet.Hosting
{
    public interface IHostingEngine
    {
        IDisposable Start(HostingContext context);
    }
}