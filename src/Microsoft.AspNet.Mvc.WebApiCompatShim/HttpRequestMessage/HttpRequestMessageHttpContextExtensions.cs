// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public static class HttpRequestMessageHttpContextExtensions
    {
        public static HttpRequestMessage GetHttpRequestMessage(this HttpContext httpContext)
        {
            var feature = httpContext.GetFeature<IHttpRequestMessageFeature>();
            if (feature == null)
            {
                feature = new HttpRequestMessageFeature(httpContext);
                httpContext.SetFeature(feature);
            }

            return feature.HttpRequestMessage;
        }
    }
}
