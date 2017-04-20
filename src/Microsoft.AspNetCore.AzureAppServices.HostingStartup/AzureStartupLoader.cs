// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.AzureAppServices.HostingStartup.AzureAppServicesHostingStartup))]

// To be able to build as <OutputType>Exe</OutputType>
internal class Program { public static void Main() { } }

namespace Microsoft.AspNetCore.AzureAppServices.HostingStartup
{
    /// <summary>
    /// A dynamic azure lightup experiance
    /// </summary>
    public class AzureAppServicesHostingStartup : IHostingStartup
    {
        /// <summary>
        /// Calls UseAzureAppServices
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(IWebHostBuilder builder)
        {
            builder.UseAzureAppServices();
        }
    }
}
