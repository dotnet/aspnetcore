using System;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupLoaderProvider : IStartupLoaderProvider
    {
        private readonly IServiceProvider _services;

        public StartupLoaderProvider(IServiceProvider services)
        {
            _services = services;
        }

        public int Order { get { return -100; } }

        public IStartupLoader CreateStartupLoader(IStartupLoader next)
        {
            return new StartupLoader(_services, next);
        }
    }
}
