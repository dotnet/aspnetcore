// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;

internal sealed class MvcRazorRuntimeCompilationOptionsSetup : IConfigureOptions<MvcRazorRuntimeCompilationOptions>
{
    private readonly IWebHostEnvironment _hostingEnvironment;

    public MvcRazorRuntimeCompilationOptionsSetup(IWebHostEnvironment hostingEnvironment)
    {
        _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
    }

    public void Configure(MvcRazorRuntimeCompilationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.FileProviders.Add(_hostingEnvironment.ContentRootFileProvider);
    }
}
