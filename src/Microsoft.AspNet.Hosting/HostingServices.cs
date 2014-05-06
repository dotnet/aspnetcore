// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.Collections.Generic;
using Microsoft.AspNet.Hosting.Builder;
using Microsoft.AspNet.Hosting.Server;
using Microsoft.AspNet.Hosting.Startup;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;

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

            yield return new ServiceDescriptor
            {
                ServiceType = typeof(IContextAccessor<>),
                ImplementationType = typeof(ContextAccessor<>),
                Lifecycle = LifecycleKind.Scoped
            };

            if (PlatformHelper.IsMono)
            {
#if NET45
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