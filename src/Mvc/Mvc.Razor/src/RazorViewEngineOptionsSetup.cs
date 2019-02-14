// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    internal class RazorViewEngineOptionsSetup :
        ConfigureCompatibilityOptions<RazorViewEngineOptions>, 
        IConfigureOptions<RazorViewEngineOptions>
    {
        private readonly IHostingEnvironment _hostingEnvironment;

        public RazorViewEngineOptionsSetup(
            IHostingEnvironment hostingEnvironment,
            ILoggerFactory loggerFactory,
            IOptions<MvcCompatibilityOptions> compatibilityOptions)
            : base(loggerFactory, compatibilityOptions)
        {
            _hostingEnvironment = hostingEnvironment ?? throw new ArgumentNullException(nameof(hostingEnvironment));
        }

        protected override IReadOnlyDictionary<string, object> DefaultValues
        {
            get
            {
                var values = new Dictionary<string, object>();
                if (Version < CompatibilityVersion.Version_2_2)
                {
                    // Default to true in 2.1 or earlier. In 2.2, we have to conditionally enable this
                    // and consequently this switch has no default value.
                    values[nameof(RazorViewEngineOptions.AllowRecompilingViewsOnFileChange)] = true;
                }

                return values;
            }
        }

        public void Configure(RazorViewEngineOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (_hostingEnvironment.ContentRootFileProvider != null)
            {
                options.FileProviders.Add(_hostingEnvironment.ContentRootFileProvider);
            }

            options.ViewLocationFormats.Add("/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
            options.ViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/{1}/{0}" + RazorViewEngine.ViewExtension);
            options.AreaViewLocationFormats.Add("/Areas/{2}/Views/Shared/{0}" + RazorViewEngine.ViewExtension);
            options.AreaViewLocationFormats.Add("/Views/Shared/{0}" + RazorViewEngine.ViewExtension);

            if (_hostingEnvironment.IsDevelopment())
            {
                options.AllowRecompilingViewsOnFileChange = true;
            }
        }
    }
}