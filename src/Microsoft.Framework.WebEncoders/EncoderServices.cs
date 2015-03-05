// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.WebEncoders
{
    public static class EncoderServices
    {
        public static IServiceCollection GetDefaultServices()
        {
            var services = new ServiceCollection();

            // Register the default encoders
            // We want to call the 'Default' property getters lazily since they perform static caching
            services.AddSingleton<IHtmlEncoder>(CreateFactory(() => HtmlEncoder.Default, filter => new HtmlEncoder(filter)));
            services.AddSingleton<IJavaScriptStringEncoder>(CreateFactory(() => JavaScriptStringEncoder.Default, filter => new JavaScriptStringEncoder(filter)));
            services.AddSingleton<IUrlEncoder>(CreateFactory(() => UrlEncoder.Default, filter => new UrlEncoder(filter)));

            return services;
        }

        private static Func<IServiceProvider, T> CreateFactory<T>(Func<T> defaultFactory, Func<ICodePointFilter, T> customFilterFactory)
        {
            return serviceProvider =>
            {
                var codePointFilter = serviceProvider?.GetService<IOptions<WebEncoderOptions>>()?.Options?.CodePointFilter;
                return (codePointFilter != null) ? customFilterFactory(codePointFilter) : defaultFactory();
            };
        }
    }
}
