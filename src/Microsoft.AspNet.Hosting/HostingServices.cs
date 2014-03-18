using System.Collections.Generic;
using Microsoft.AspNet.ConfigurationModel;
using Microsoft.AspNet.DependencyInjection;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Security.DataProtection;

namespace Microsoft.AspNet.Hosting
{
    public static class HostingServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices()
        {
            return GetDefaultServices(new Configuration());
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration)
        {
            var describer = new ServiceDescriber(configuration);

            yield return describer.Transient<IHostingEngine, HostingEngine>();
            yield return describer.Transient<IServerManager, ServerManager>();

            yield return describer.Transient<IStartupManager, StartupManager>();
            yield return describer.Transient<IStartupLoaderProvider, StartupLoaderProvider>();

            yield return describer.Transient<IBuilderFactory, BuilderFactory>();
            yield return describer.Transient<IHttpContextFactory, HttpContextFactory>();

            yield return describer.Transient<ITypeActivator, TypeActivator>();

            // The default IDataProtectionProvider is a singleton.
            // Note: DPAPI isn't usable in IIS where the user profile hasn't been loaded, but loading DPAPI
            // is deferred until the first call to Protect / Unprotect. It's up to an IIS-based host to
            // replace this service as part of application initialization.
            yield return describer.Instance<IDataProtectionProvider>(DataProtectionProvider.CreateFromDpapi());
        }
    }
}