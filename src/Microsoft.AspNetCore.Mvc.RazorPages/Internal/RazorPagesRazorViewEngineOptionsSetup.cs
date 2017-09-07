// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorPagesRazorViewEngineOptionsSetup : IConfigureOptions<RazorViewEngineOptions>
    {
        private readonly RazorPagesOptions _pagesOptions;

        public RazorPagesRazorViewEngineOptionsSetup(IOptions<RazorPagesOptions> pagesOptions)
        {
            _pagesOptions = pagesOptions?.Value ?? throw new ArgumentNullException(nameof(pagesOptions));
        }

        public void Configure(RazorViewEngineOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var rootDirectory = _pagesOptions.RootDirectory;
            Debug.Assert(!string.IsNullOrEmpty(rootDirectory));
            var defaultPageSearchPath = CombinePath(rootDirectory, "{1}/{0}");
            options.PageViewLocationFormats.Add(defaultPageSearchPath);

            // /Pages/Shared/{0}.cshtml
            var pagesSharedDirectory = CombinePath(rootDirectory, "Shared/{0}");
            options.PageViewLocationFormats.Add(pagesSharedDirectory);

            options.PageViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

            options.ViewLocationFormats.Add(pagesSharedDirectory);
            options.AreaViewLocationFormats.Add(pagesSharedDirectory);

            options.ViewLocationExpanders.Add(new PageViewLocationExpander());
        }

        private static string CombinePath(string path1, string path2)
        {
            if (path1.EndsWith("/", StringComparison.Ordinal))
            {
                return path1 + path2 + RazorViewEngine.ViewExtension;
            }

            return path1 + "/" + path2 + RazorViewEngine.ViewExtension;
        }
    }
}
