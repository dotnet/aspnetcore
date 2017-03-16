// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class RazorPagesOptionsSetup : IConfigureOptions<RazorPagesOptions>
    {
        public void Configure(RazorPagesOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            // Support for [TempData] on properties
            options.ConfigureFilter(new SaveTempDataPropertyFilterFactory());
            // Always require an antiforgery token on post
            options.ConfigureFilter(new AutoValidateAntiforgeryTokenAttribute());
        }
    }
}
