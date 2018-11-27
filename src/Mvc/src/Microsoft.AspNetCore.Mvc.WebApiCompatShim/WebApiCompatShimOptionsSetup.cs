// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Net.Http.Formatting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    public class WebApiCompatShimOptionsSetup
        : IConfigureOptions<MvcOptions>, IConfigureOptions<WebApiCompatShimOptions>
    {
        public static readonly string DefaultAreaName = "api";

        public string Name { get; set; }

        public void Configure(MvcOptions options)
        {
            // Add webapi behaviors to controllers with the appropriate attributes
            options.Conventions.Add(new WebApiActionConventionsApplicationModelConvention());
            options.Conventions.Add(new WebApiParameterConventionsApplicationModelConvention());
            options.Conventions.Add(new WebApiOverloadingApplicationModelConvention());
            options.Conventions.Add(new WebApiRoutesApplicationModelConvention(area: DefaultAreaName));

            // Add an action filter for handling the HttpResponseException.
            options.Filters.Add(new HttpResponseExceptionActionFilter());

            // Add a model binder to be able to bind HttpRequestMessage
            options.ModelBinderProviders.Insert(0, new HttpRequestMessageModelBinderProvider());

            // Add a formatter to write out an HttpResponseMessage to the response
            options.OutputFormatters.Insert(0, new HttpResponseMessageOutputFormatter());

            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(HttpRequestMessage)));
            options.ModelMetadataDetailsProviders.Add(new SuppressChildValidationMetadataProvider(typeof(HttpResponseMessage)));
        }

        public void Configure(WebApiCompatShimOptions options)
        {
            // Add the default formatters
            options.Formatters.AddRange(new MediaTypeFormatterCollection());
        }
    }
}
