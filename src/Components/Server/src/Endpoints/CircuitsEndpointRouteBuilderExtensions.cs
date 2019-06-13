// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder
{
    internal static class CircuitsEndpointRouteBuilderExtensions
    {
        public static IEndpointConventionBuilder MapStartCircuitEndpoint(this IEndpointRouteBuilder builder, string pattern)
        {
            var circuitIdFactory = builder.ServiceProvider.GetRequiredService<CircuitIdFactory>();
            var actions = new CircuitEndpoints(circuitIdFactory);
            IApplicationBuilder start = builder.CreateApplicationBuilder();
            start.Run(actions.StartCircuitAsync);
            IEndpointConventionBuilder startBuilder = builder.Map(pattern, start.Build());
            return startBuilder;
        }
    }

    internal class CircuitEndpoints
    {
        private CircuitIdFactory _circuitIdFactory;

        public CircuitEndpoints(CircuitIdFactory circuitIdFactory)
        {
            _circuitIdFactory = circuitIdFactory;
        }

        public async Task StartCircuitAsync(HttpContext context)
        {
            var id = _circuitIdFactory.CreateCircuitId();
            var authService = context.RequestServices.GetRequiredService<IAuthenticationService>();
            var properties = new AuthenticationProperties();
            properties.Items["RequestToken"] = id.RequestToken;
            properties.Items["CookieToken"] = id.CookieToken;

            var principal = new ClaimsPrincipal();
            principal.AddIdentity(new ClaimsIdentity("CircuitId"));
            await authService.SignInAsync(
                context,
                "CircuitId",
                principal,
                properties);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json;charset=utf-8";
            await context.Response.WriteAsync(JsonSerializer.ToString(new RequestCircuitId { Id = id.RequestToken }));
        }

        private class RequestCircuitId
        {
            public string Id { get; set; }
        }
    }
}
