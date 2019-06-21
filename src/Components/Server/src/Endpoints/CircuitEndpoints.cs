// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Builder
{
    internal class CircuitEndpoints
    {
        private readonly CircuitIdFactory _circuitIdFactory;
        private static readonly JsonSerializerOptions _jsonSerializationOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public CircuitEndpoints(CircuitIdFactory circuitIdFactory)
        {
            _circuitIdFactory = circuitIdFactory;
        }

        public async Task StartCircuitAsync(HttpContext context)
        {
            // We only accept post so that cross-site requests are forced to go through a preflight.
            if (!HttpMethods.IsPost(context.Request.Method))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var id = _circuitIdFactory.CreateCircuitId();
            await CircuitMatcherPolicy.AttachCircuitIdAsync(context, id);

            var circuitId = new RequestCircuitId { Id = id.RequestToken };

            var response = context.Response;
            response.StatusCode = 200;
            response.ContentType = "application/json;charset=utf-8";
            await JsonSerializer.WriteAsync(response.Body, circuitId, _jsonSerializationOptions);
        }

        private struct RequestCircuitId
        {
            public string Id { get; set; }
        }
    }
}
