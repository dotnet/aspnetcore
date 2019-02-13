// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace Microsoft.AspNetCore.Mvc.WebApiCompatShim
{
    public static class HttpRequestMessageHttpContextExtensions
    {
        public static HttpRequestMessage GetHttpRequestMessage(this HttpContext httpContext)
        {
            var feature = httpContext.Features.Get<IHttpRequestMessageFeature>();
            if (feature == null)
            {
                feature = new HttpRequestMessageFeature(httpContext);
                httpContext.Features.Set(feature);
            }

            return feature.HttpRequestMessage;
        }
    }
}
