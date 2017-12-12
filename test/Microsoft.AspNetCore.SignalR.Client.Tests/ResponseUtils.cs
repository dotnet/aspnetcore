// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text;

using SocketsTransportType = Microsoft.AspNetCore.Sockets.TransportType;

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

        public static bool IsNegotiateRequest(HttpRequestMessage request)
        {
            return request.Method == HttpMethod.Post &&
                new UriBuilder(request.RequestUri).Path.EndsWith("/negotiate");
        }

        public static string CreateNegotiationResponse(string connectionId = "00000000-0000-0000-0000-000000000000",
            SocketsTransportType? transportTypes = SocketsTransportType.All)
        {
            var sb = new StringBuilder("{ ");
            if (connectionId != null)
            {
                sb.Append($"\"connectionId\": \"{connectionId}\",");
            }
            if (transportTypes != null)
            {
                sb.Append($"\"availableTransports\": [ ");
                if ((transportTypes & SocketsTransportType.WebSockets) == SocketsTransportType.WebSockets)
                {
                    sb.Append($"\"{nameof(SocketsTransportType.WebSockets)}\",");
                }
                if ((transportTypes & SocketsTransportType.ServerSentEvents) == SocketsTransportType.ServerSentEvents)
                {
                    sb.Append($"\"{nameof(SocketsTransportType.ServerSentEvents)}\",");
                }
                if ((transportTypes & SocketsTransportType.LongPolling) == SocketsTransportType.LongPolling)
                {
                    sb.Append($"\"{nameof(SocketsTransportType.LongPolling)}\",");
                }
                sb.Length--;
                sb.Append("],");
            }
            sb.Length--;
            sb.Append("}");

            return sb.ToString();
        }
    }
}
