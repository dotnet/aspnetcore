// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

internal sealed class RazorRuntimeCompilationHostingStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        // Add Razor services
#pragma warning disable ASPDEPR003 // Type or member is obsolete
        builder.ConfigureServices(RazorRuntimeCompilationMvcCoreBuilderExtensions.AddServices);
#pragma warning restore ASPDEPR003 // Type or member is obsolete
    }
}
