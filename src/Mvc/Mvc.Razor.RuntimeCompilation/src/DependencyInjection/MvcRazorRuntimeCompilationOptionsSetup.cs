// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

#pragma warning disable ASPDEPR003 // Type or member is obsolete
internal sealed class MvcRazorRuntimeCompilationOptionsSetup : IConfigureOptions<MvcRazorRuntimeCompilationOptions>
#pragma warning restore ASPDEPR003 // Type or member is obsolete
{
    private readonly IWebHostEnvironment _hostingEnvironment;

    public MvcRazorRuntimeCompilationOptionsSetup(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
    }

#pragma warning disable ASPDEPR003 // Type or member is obsolete
    public void Configure(MvcRazorRuntimeCompilationOptions options)
#pragma warning restore ASPDEPR003 // Type or member is obsolete
    {
        ArgumentNullException.ThrowIfNull(options);

        options.FileProviders.Add(_hostingEnvironment.ContentRootFileProvider);
    }
}
