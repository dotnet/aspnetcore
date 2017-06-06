// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Microsoft.AspNetCore.Client.Tests
{
    internal static class ResponseUtils
    {
        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode) =>
            CreateResponse(statusCode, string.Empty);

        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string payload) =>
            CreateResponse(statusCode, new StringContent(payload));

        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, byte[] payload) =>
            CreateResponse(statusCode, new ByteArrayContent(payload));

        public static HttpResponseMessage CreateResponse(HttpStatusCode statusCode, HttpContent payload)
        {
            return new HttpResponseMessage(statusCode)
            {
                Content = payload
            };
        }
    }
}
