using System;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.DependencyInjection.Fallback;

namespace Microsoft.AspNet.Hosting
{
    public static class WebApplication
    {
        public static IDisposable Start()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.Add(HostingServices.GetDefaultServices());

            var context = new HostingContext
            {
                Services = serviceCollection.BuildServiceProvider()
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