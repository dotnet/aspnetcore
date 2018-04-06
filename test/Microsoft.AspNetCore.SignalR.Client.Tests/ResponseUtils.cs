// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Connections.Internal;
using Newtonsoft.Json;

namespace Microsoft.AspNetCore.SignalR.Client.Tests
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
            HttpTransportType? transportTypes = null)
        {
            var availableTransports = new List<object>();

            transportTypes = transportTypes ?? HttpTransports.All;
            if ((transportTypes & HttpTransportType.WebSockets) != 0)
            {
                availableTransports.Add(new
                {
                    transport = nameof(HttpTransportType.WebSockets),
                    transferFormats = new[] { nameof(TransferFormat.Text), nameof(TransferFormat.Binary) }
                });
            }
            if ((transportTypes & HttpTransportType.ServerSentEvents) != 0)
            {
                availableTransports.Add(new
                {
                    transport = nameof(HttpTransportType.ServerSentEvents),
                    transferFormats = new[] { nameof(TransferFormat.Text) }
                });
            }
            if ((transportTypes & HttpTransportType.LongPolling) != 0)
            {
                availableTransports.Add(new
                {
                    transport = nameof(HttpTransportType.LongPolling),
                    transferFormats = new[] { nameof(TransferFormat.Text), nameof(TransferFormat.Binary) }
                });
            }

            return JsonConvert.SerializeObject(new { connectionId, availableTransports });
        }
    }
}
