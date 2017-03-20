// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Sockets.Internal.Formatters;

namespace Microsoft.AspNetCore.Client.Tests
{
    internal static class ResponseUtils
    {
        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode) =>
            CreateResponse(statusCode, MessageFormatter.TextContentType, string.Empty);

        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string contentType, string payload) =>
            CreateResponse(statusCode, contentType, new StringContent(payload));

        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string contentType, byte[] payload) =>
            CreateResponse(statusCode, contentType, new ByteArrayContent(payload));

        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string contentType, HttpContent payload)
        {
            payload.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            return new HttpResponseMessage(statusCode)
            {
                Content = payload
            };
        }
    }
}
