// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

internal sealed class RazorPagesRazorViewEngineOptionsSetup : IConfigureOptions<RazorViewEngineOptions>
{
    private readonly RazorPagesOptions _pagesOptions;

    public RazorPagesRazorViewEngineOptionsSetup(IOptions<RazorPagesOptions> pagesOptions)
    {
        _pagesOptions = pagesOptions?.Value ?? throw new ArgumentNullException(nameof(pagesOptions));
    }

    public void Configure(RazorViewEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var rootDirectory = _pagesOptions.RootDirectory;
        Debug.Assert(!string.IsNullOrEmpty(rootDirectory));
        var defaultPageSearchPath = CombinePath(rootDirectory, "{1}/{0}" + RazorViewEngine.ViewExtension);
        options.PageViewLocationFormats.Add(defaultPageSearchPath);

        // /Pages/Shared/{0}.cshtml
        var pagesSharedDirectory = CombinePath(rootDirectory, "Shared/{0}" + RazorViewEngine.ViewExtension);
        options.PageViewLocationFormats.Add(pagesSharedDirectory);

        options.PageViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

        var areaDirectory = CombinePath("/Areas/", "{2}");
        // Areas/{2}/Pages/
        var areaPagesDirectory = CombinePath(areaDirectory, "/Pages/");

        // Areas/{2}/Pages/{1}/{0}.cshtml
        // Areas/{2}/Pages/Shared/{0}.cshtml
        // Areas/{2}/Views/Shared/{0}.cshtml
        // Pages/Shared/{0}.cshtml
        // Views/Shared/{0}.cshtml
        var areaSearchPath = CombinePath(areaPagesDirectory, "{1}/{0}" + RazorViewEngine.ViewExtension);
        options.AreaPageViewLocationFormats.Add(areaSearchPath);

        var areaPagesSharedSearchPath = CombinePath(areaPagesDirectory, "Shared/{0}" + RazorViewEngine.ViewExtension);
        options.AreaPageViewLocationFormats.Add(areaPagesSharedSearchPath);

        var areaViewsSharedSearchPath = CombinePath(areaDirectory, "Views/Shared/{0}" + RazorViewEngine.ViewExtension);
        options.AreaPageViewLocationFormats.Add(areaViewsSharedSearchPath);

        options.AreaPageViewLocationFormats.Add(pagesSharedDirectory);
        options.AreaPageViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

        options.ViewLocationFormats.Add(pagesSharedDirectory);
        options.AreaViewLocationFormats.Add(pagesSharedDirectory);

        options.ViewLocationExpanders.Add(new PageViewLocationExpander());
    }

    private static string CombinePath(string path1, string path2)
    {
        if (path1.EndsWith('/') || path2.StartsWith('/'))
        {
            return path1 + path2;
        }
        else if (path1.EndsWith('/') && path2.StartsWith('/'))
        {
            return string.Concat(path1, path2.AsSpan(1));
        }

        return path1 + "/" + path2;
    }
}
