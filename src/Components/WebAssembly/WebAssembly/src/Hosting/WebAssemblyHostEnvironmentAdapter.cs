// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

internal sealed class WebAssemblyHostEnvironmentAdapter : IHostEnvironment
{
    private readonly IWebAssemblyHostEnvironment _webAssemblyHostEnvironment;

    public WebAssemblyHostEnvironmentAdapter(IWebAssemblyHostEnvironment webAssemblyHostEnvironment)
    {
        _webAssemblyHostEnvironment = webAssemblyHostEnvironment;
    }

    public string EnvironmentName
    {
        get => _webAssemblyHostEnvironment.Environment;
        set => throw new NotSupportedException("Setting the environment name is not supported in WebAssembly.");
    }

    public string ApplicationName
    {
        get => string.Empty;
        set => throw new NotSupportedException("Setting the application name is not supported in WebAssembly.");
    }

    public string ContentRootPath
    {
        get => string.Empty;
        set => throw new NotSupportedException("Setting the content root path is not supported in WebAssembly.");
    }

    public IFileProvider ContentRootFileProvider
    {
        get => new NullFileProvider();
        set => throw new NotSupportedException("Setting the content root file provider is not supported in WebAssembly.");
    }
}
