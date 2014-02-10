using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Hosting.Startup
{
    public class StartupManager : IStartupManager
    {
        private readonly IEnumerable<IStartupLoaderProvider> _providers;

        public StartupManager(IEnumerable<IStartupLoaderProvider> providers)
        {
            _providers = providers;
        }

        public Action<IBuilder> LoadStartup(string applicationName)
        {
            // build ordered chain of application loaders
            var chain = _providers
                .OrderBy(provider => provider.Order)
                .Aggregate(NullStartupLoader.Instance, (next, provider) => provider.CreateStartupLoader(next));

            // invoke chain to acquire application entrypoint and diagnostic messages
            var diagnosticMessages = new List<string>();
            var application = chain.LoadStartup(applicationName, diagnosticMessages);

            if (application == null)
            {
                throw new Exception(diagnosticMessages.Aggregate("TODO: web application entrypoint not found message", (a, b) => a + "\r\n" + b));
            }

            return application;
        }
    }
}