// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class RazorPagesOptionsSetup : IConfigureOptions<RazorPagesOptions>
    {
        private readonly MvcOptions _mvcOptions;

        public RazorPagesOptionsSetup(IOptions<MvcOptions> pagesOptions)
        {
            _mvcOptions = pagesOptions?.Value ?? throw new ArgumentNullException(nameof(pagesOptions));
        }

        public void Configure(RazorPagesOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            options.Conventions = new PageConventionCollection(_mvcOptions);
        }
    }
}
