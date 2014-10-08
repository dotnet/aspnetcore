// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;

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

            yield return describer.Transient<IApplicationBuilderFactory, ApplicationBuilderFactory>();
            yield return describer.Transient<IHttpContextFactory, HttpContextFactory>();

            yield return describer.Singleton<ITypeActivator, TypeActivator>();

            yield return describer.Instance<IApplicationLifetime>(new ApplicationLifetime());

            // TODO: Do we expect this to be provide by the runtime eventually?
            yield return describer.Singleton<ILoggerFactory, LoggerFactory>();

            yield return describer.Scoped(typeof(IContextAccessor<>), typeof(ContextAccessor<>));

            if (PlatformHelper.IsMono)
            {
#if ASPNET50
                yield return describer.Instance<IDataProtectionProvider>(DataProtectionProvider.CreateFromLegacyDpapi());
#endif
            }
            else
            {
                // The default IDataProtectionProvider is a singleton.
                // Note: DPAPI isn't usable in IIS where the user profile hasn't been loaded, but loading DPAPI
                // is deferred until the first call to Protect / Unprotect. It's up to an IIS-based host to
                // replace this service as part of application initialization.
                yield return describer.Instance<IDataProtectionProvider>(DataProtectionProvider.CreateFromDpapi());
            }
        }
    }
}
