// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Network;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public static class ResponseAssert
    {
        public static Uri IsRedirect(HttpResponseMessage responseMessage)
        {
            Assert.Equal(HttpStatusCode.Redirect, responseMessage.StatusCode);
            return responseMessage.Headers.Location;
        }

        public static async Task<IHtmlDocument> IsHtmlDocumentAsync(HttpResponseMessage response)
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType.MediaType);
            var content = await response.Content.ReadAsStringAsync();
            var document = await BrowsingContext.New()
                .OpenAsync(ResponseFactory, CancellationToken.None);
            return Assert.IsAssignableFrom<IHtmlDocument>(document);

            void ResponseFactory(VirtualResponse htmlResponse)
            {
                htmlResponse
                    .Address(response.RequestMessage.RequestUri)
                    .Status(response.StatusCode);

                MapHeaders(response.Headers);
                MapHeaders(response.Content.Headers);

                htmlResponse.Content(content);

                void MapHeaders(HttpHeaders headers){
                    foreach (var header in headers)
                    {
                        foreach (var value in header.Value)
                        {
                            htmlResponse.Header(header.Key, value);
                        }
                    }
                }
            }
        }

        internal static void IsOK(HttpResponseMessage download)
        {
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        }
    }
}
