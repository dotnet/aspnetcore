// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Configuration;
using System.Web;
using System.Web.Configuration;
using Microsoft.AspNetCore.DataProtection.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.DataProtection.SystemWeb
{
    /// <summary>
    /// Allows controlling the configuration of the ASP.NET Core Data Protection system.
    /// </summary>
    /// <remarks>
    /// Developers should not call these APIs directly. Instead, developers should subclass
    /// this type and override the <see cref="ConfigureServices(IServiceCollection)"/>
    /// method or <see cref="CreateDataProtectionProvider(IServiceProvider)"/> methods
    /// as appropriate.
    /// </remarks>
    public class DataProtectionStartup
    {
        /// <summary>
        /// Configures services used by the Data Protection system.
        /// </summary>
        /// <param name="services">A mutable collection of services.</param>
        /// <remarks>
        /// Developers may override this method to change the default behaviors of
        /// the Data Protection system.
        /// </remarks>
        public virtual void ConfigureServices(IServiceCollection services)
        {
            // InternalConfigureServices already takes care of default configuration.
            // The reason we don't configure default logic in this method is that we don't
            // want to punish the developer for forgetting to call base.ConfigureServices
            // from within his own override.
        }

        /// <summary>
        /// Creates a new instance of an <see cref="IDataProtectionProvider"/>.
        /// </summary>
        /// <param name="services">A collection of services from which to create the <see cref="IDataProtectionProvider"/>.</param>
        /// <returns>An <see cref="IDataProtectionProvider"/>.</returns>
        /// <remarks>
        /// Developers should generally override the <see cref="ConfigureServices(IServiceCollection)"/>
        /// method instead of this method.
        /// </remarks>
        public virtual IDataProtectionProvider CreateDataProtectionProvider(IServiceProvider services)
        {
            return services.GetDataProtectionProvider();
        }

        /// <summary>
        /// Provides a default implementation of required services, calls the developer's
        /// configuration overrides, then creates an <see cref="IDataProtectionProvider"/>.
        /// </summary>
        internal IDataProtectionProvider InternalConfigureServicesAndCreateProtectionProvider()
        {
            // Configure the default implementation, passing in our custom discriminator
            var services = new ServiceCollection();
            services.AddDataProtection();
            services.AddSingleton<IApplicationDiscriminator>(new SystemWebApplicationDiscriminator());

            // Run user-specified configuration and get an instance of the provider
            ConfigureServices(services);
            var provider = CreateDataProtectionProvider(services.BuildServiceProvider());
            if (provider == null)
            {
                throw new InvalidOperationException(Resources.Startup_CreateProviderReturnedNull);
            }

            // And we're done!
            return provider;
        }

        private sealed class SystemWebApplicationDiscriminator : IApplicationDiscriminator
        {
            private readonly Lazy<string> _lazyDiscriminator = new Lazy<string>(GetAppDiscriminatorCore);

            public string Discriminator => _lazyDiscriminator.Value;

            private static string GetAppDiscriminatorCore()
            {
                // Try reading the discriminator from <machineKey applicationName="..." /> defined
                // at the web app root. If the value was set explicitly (even if the value is empty),
                // honor it as the discriminator.
                var machineKeySection = (MachineKeySection)WebConfigurationManager.GetWebApplicationSection("system.web/machineKey");
                if (machineKeySection.ElementInformation.Properties["applicationName"].ValueOrigin != PropertyValueOrigin.Default)
                {
                    return machineKeySection.ApplicationName;
                }
                else
                {
                    // Otherwise, fall back to the IIS metabase config path.
                    // This is unique per machine.
                    return HttpRuntime.AppDomainAppId;
                }
            }
        }
    }
}
