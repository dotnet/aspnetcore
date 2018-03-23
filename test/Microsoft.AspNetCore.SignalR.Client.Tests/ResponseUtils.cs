// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Sockets;
using Newtonsoft.Json;
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

        public static string CreateNegotiationContent(string connectionId = "00000000-0000-0000-0000-000000000000",
            SocketsTransportType transportTypes = SocketsTransportType.All)
        {
            var availableTransports = new List<object>();

            if ((transportTypes & SocketsTransportType.WebSockets) != 0)
            {
                availableTransports.Add(new
                {
                    transport = nameof(SocketsTransportType.WebSockets),
                    transferFormats = new[] { nameof(TransferFormat.Text), nameof(TransferFormat.Binary) }
                });
            }
            if ((transportTypes & SocketsTransportType.ServerSentEvents) != 0)
            {
                availableTransports.Add(new
                {
                    transport = nameof(SocketsTransportType.ServerSentEvents),
                    transferFormats = new[] { nameof(TransferFormat.Text) }
                });
            }
            if ((transportTypes & SocketsTransportType.LongPolling) != 0)
            {
                availableTransports.Add(new
                {
                    transport = nameof(SocketsTransportType.LongPolling),
                    transferFormats = new[] { nameof(TransferFormat.Text), nameof(TransferFormat.Binary) }
                });
            }

            return JsonConvert.SerializeObject(new { connectionId, availableTransports });
        }
    }
}
