// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public class HttpRequestMessageFeature : IHttpRequestMessageFeature
    {
        private readonly HttpContext _httpContext;
        private HttpRequestMessage _httpRequestMessage;

        public HttpRequestMessageFeature([NotNull] HttpContext httpContext)
        {
            _httpContext = httpContext;
        }

        public HttpRequestMessage HttpRequestMessage
        {
            get
            {
                if (_httpRequestMessage == null)
                {
                    _httpRequestMessage = CreateHttpRequestMessage(_httpContext);
                }

                return _httpRequestMessage;
            }

            set
            {
                _httpRequestMessage = value;
            }
        }

        private static HttpRequestMessage CreateHttpRequestMessage(HttpContext httpContext)
        {
            var httpRequest = httpContext.Request;
            var uriString =
                httpRequest.Scheme + "://" +
                httpRequest.Host +
                httpRequest.PathBase +
                httpRequest.Path +
                httpRequest.QueryString;

            var message = new HttpRequestMessage(new HttpMethod(httpRequest.Method), uriString);

            // This allows us to pass the message through APIs defined in legacy code and then
            // operate on the HttpContext inside.
            message.Properties[nameof(HttpContext)] = httpContext;

            message.Content = new StreamContent(httpRequest.Body);

            foreach (var header in httpRequest.Headers)
            {
                // Every header should be able to fit into one of the two header collections.
                // Try message.Headers first since that accepts more of them.
                if (!message.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value))
                {
                    var added = message.Content.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
                    Debug.Assert(added);
                }
            }

            return message;
        }
    }
}
