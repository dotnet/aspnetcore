// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor;

internal sealed class RazorViewEngineOptionsSetup : IConfigureOptions<RazorViewEngineOptions>
{
    public void Configure(RazorViewEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.ViewLocationFormats.Add("/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
        options.ViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

        options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
        options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
        options.AreaViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
    }
}
