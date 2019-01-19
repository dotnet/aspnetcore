// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.AzureAppServices.HostingStartup.AzureAppServicesHostingStartup))]

namespace Microsoft.AspNetCore.AzureAppServices.HostingStartup
{
    /// <summary>
    /// A dynamic azure lightup experience
    /// </summary>
    public class AzureAppServicesHostingStartup : IHostingStartup
    {
        private const string HostingStartupName = "AppServices";
        private const string DiagnosticsFeatureName = "DiagnosticsEnabled";

        /// <summary>
        /// Calls UseAzureAppServices
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(IWebHostBuilder builder)
        {
            var baseConfiguration = HostingStartupConfigurationExtensions.GetBaseConfiguration();

            if (baseConfiguration.IsEnabled(HostingStartupName, DiagnosticsFeatureName))
            {
                builder.UseAzureAppServices();
            }
        }
    }
}
