// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorPagesRazorViewEngineOptionsSetup : IConfigureOptions<RazorViewEngineOptions>
    {
        private readonly IOptions<RazorPagesOptions> _pagesOptions;

        public RazorPagesRazorViewEngineOptionsSetup(IOptions<RazorPagesOptions> pagesOptions)
        {
            _pagesOptions = pagesOptions;
        }

        public void Configure(RazorViewEngineOptions options)
        {
            Debug.Assert(_pagesOptions.Value.RootDirectory.Length > 0);

            if (_pagesOptions.Value.RootDirectory == "/")
            {
                options.PageViewLocationFormats.Add("/{1}/{0}" + RazorViewEngine.ViewExtension);
            }
            else
            {
                options.PageViewLocationFormats.Add(_pagesOptions.Value.RootDirectory + "/{1}/{0}" + RazorViewEngine.ViewExtension);
            }

            options.PageViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

            options.ViewLocationExpanders.Add(new PageViewLocationExpander());
        }
    }
}
