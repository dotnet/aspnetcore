// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.ConfigurationModel;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.Framework.WebEncoders
{
    public static class EncoderServices
    {
        public static IEnumerable<IServiceDescriptor> GetDefaultServices()
        {
            return GetDefaultServices(configuration: null);
        }

        public static IEnumerable<IServiceDescriptor> GetDefaultServices(IConfiguration configuration)
        {
            var describe = new ServiceDescriber(configuration);

            // Register the default encoders
            // We want to call the 'Default' property getters lazily since they perform static caching
            yield return describe.Singleton<IHtmlEncoder>(CreateFactory(() => HtmlEncoder.Default, filter => new HtmlEncoder(filter)));
            yield return describe.Singleton<IJavaScriptStringEncoder>(CreateFactory(() => JavaScriptStringEncoder.Default, filter => new JavaScriptStringEncoder(filter)));
            yield return describe.Singleton<IUrlEncoder>(CreateFactory(() => UrlEncoder.Default, filter => new UrlEncoder(filter)));
        }

        private static Func<IServiceProvider, T> CreateFactory<T>(Func<T> defaultFactory, Func<ICodePointFilter, T> customFilterFactory)
        {
            return serviceProvider =>
            {
                var codePointFilter = serviceProvider?.GetService<IOptions<EncoderOptions>>()?.Options?.CodePointFilter;
                return (codePointFilter != null) ? customFilterFactory(codePointFilter) : defaultFactory();
            };
        }
    }
}
