// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;

[assembly: HostingStartup(typeof(Microsoft.AspNetCore.Server.IISIntegration.IISHostingStartup))]

namespace Microsoft.AspNetCore.Server.IISIntegration
{
    public class IISHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            builder.UseIISIntegration();
        }
    }
}
