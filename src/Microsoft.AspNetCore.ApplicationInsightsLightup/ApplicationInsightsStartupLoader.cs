// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.ApplicationInsightsLightup.ApplicationInsightsStartupLoader))]

namespace Microsoft.AspNetCore.ApplicationInsightsLightup
{
    /// <summary>
    /// A dynamic Application Insights lightup experiance
    /// </summary>
    public class ApplicationInsightsStartupLoader : IHostingStartup
    {
        /// <summary>
        /// Calls UseApplicationInsights
        /// </summary>
        /// <param name="builder"></param>
        public void Configure(IWebHostBuilder builder)
        {
            builder.UseApplicationInsights();
        }
    }
}
