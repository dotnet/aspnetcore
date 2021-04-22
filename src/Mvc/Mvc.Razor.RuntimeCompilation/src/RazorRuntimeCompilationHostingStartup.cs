// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
{
    internal sealed class RazorRuntimeCompilationHostingStartup : IHostingStartup
    {
        public void Configure(IWebHostBuilder builder)
        {
            // Add Razor services
            builder.ConfigureServices(services => RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices(services));
        }
    }
}
