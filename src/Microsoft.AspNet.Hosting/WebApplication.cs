using System;
using Microsoft.AspNet.DependencyInjection;

namespace Microsoft.AspNet.Hosting
{
    public static class WebApplication
    {
        public static IDisposable Start()
        {
            var context = new HostingContext
            {
                Services = new ServiceProvider().Add(HostingServices.GetDefaultServices())
            };

            var engine = context.Services.GetService<IHostingEngine>();
            if (engine == null)
            {
                throw new Exception("TODO: IHostingEngine service not available exception");
            }
            return engine.Start(context);
        }
    }
}