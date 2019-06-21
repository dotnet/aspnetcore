// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class CircuitMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
    {
        internal const string IdClaimType = "bcid";
        internal const string CookiePrefix = "Circuit.";

        private const int PrefixLenght = 4;
        private const string QueryStringParameterKey = "CircuitId";

        private readonly ConcurrentDictionary<Endpoint, Endpoint> _protectedHubEndpointCache = new ConcurrentDictionary<Endpoint, Endpoint>();

        public CircuitMatcherPolicy(
            CircuitIdFactory circuitIdFactory,
            CircuitRegistry circuitRegistry)
        {
            CircuitIdFactory = circuitIdFactory;
            CircuitRegistry = circuitRegistry;
        }

        public override int Order => short.MaxValue;

        public CircuitIdFactory CircuitIdFactory { get; }
        public CircuitRegistry CircuitRegistry { get; }

        public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                if (IsComponentHubEndpoint(endpoint))
                {
                    return true;
                }
            }

            return false;
        }

        bool IsComponentHubEndpoint(Endpoint endpoint) =>
            endpoint.Metadata.GetMetadata<HubMetadata>()?.HubType == typeof(ComponentHub);

        public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                ref var candidate = ref candidates[i];
                if (IsComponentHubEndpoint(candidate.Endpoint))
                {
                    var newEndpoint = _protectedHubEndpointCache.GetOrAdd(candidate.Endpoint, CreateProtectedEndpoint);
                    candidates.ReplaceEndpoint(i, newEndpoint, candidate.Values);
                }
            }

            return Task.CompletedTask;
        }

        private Endpoint CreateProtectedEndpoint(Endpoint endpoint)
        {
            var routeEndpoint = (RouteEndpoint)endpoint;

            // Replaces the negotiate endpoint with one that does the service redirect
            async Task ProtectedEndpoint(HttpContext context)
            {
                if (!ValidateCircuitId(context))
                {
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return;
                }

                await endpoint.RequestDelegate(context);
            }

            var routeEndpointBuilder = new RouteEndpointBuilder(
                ProtectedEndpoint,
                routeEndpoint.RoutePattern,
                routeEndpoint.Order);

            // Preserve the metadata
            foreach (var metadata in endpoint.Metadata)
            {
                routeEndpointBuilder.Metadata.Add(metadata);
            }

            return routeEndpointBuilder.Build();
        }

        private bool ValidateCircuitId(HttpContext context)
        {
            if (!context.Request.Query.TryGetValue(QueryStringParameterKey, out var circuitId) || circuitId.Count != 1)
            {
                // There is no circuit id on the query string, so we don't authenticate anything.
                return false;
            }

            // Signalr requires clients to send the header X-Requested-With or fails.
            // This means that it will force cross-origin requests to go through CORS
            // which means we will see a preflight request, in which case we fail.
            if (HttpMethods.IsOptions(context.Request.Method) &&
                context.Request.Headers.ContainsKey(HeaderNames.AccessControlRequestMethod))
            {
                return false;
            }

            var key = $"{CookiePrefix}{GetCircuitIdPrefix(circuitId)}";

            if (context.Request.Cookies.TryGetValue(key, out var cookie))
            {
                if (CircuitIdFactory.ValidateCircuitId(circuitId, cookie, out var parsedId))
                {
                    CleanupStaleCookies(context);

                    var identity = new ClaimsIdentity();
                    identity.AddClaim(new Claim(IdClaimType, parsedId.RequestToken));
                    context.User.AddIdentity(identity);

                    return true;
                }
            }

            return false;
        }

        // Blazor requires stickyness as the circuit is held in memory.
        // That means that on scale-out scenarios there must be a session cookie and all circuits for a given
        // session will end up in the same server.
        // With this information, we can simply check the cookies the client sends us and discard any cookie for
        // which we can't find a circuit, either connected or disconnected, as when the user closes the browser
        // all sessions (and whatever affinity cookie was set) will go away.
        // Circuits for which there isn't an already created circuit will contain an additional KeepAlive cookie
        // that will be used as a filter to avoid deleting the circuit id cookie. The KeepAlive cookie for a given
        // circuit will go away automatically.
        private void CleanupStaleCookies(HttpContext context)
        {
            var cookieCollection = context.Request.Cookies;

            foreach (var cookieName in cookieCollection.Keys)
            {
                var options = CreateCookieOptions(context, cookieName);
                if (cookieName.StartsWith(CookiePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        var id = CircuitIdFactory.FromCookieValue(cookieCollection[cookieName]);
                        if (!CircuitRegistry.ContainsCircuit(id.RequestToken) &&
                            !cookieCollection.ContainsKey(GetKeepAliveCookieName(cookieName)))
                        {
                            context.Response.Cookies.Delete(cookieName, options);
                        }
                    }
                    catch
                    {
                        context.Response.Cookies.Delete(cookieName, options);
                    }
                }
            }
        }

        private static Task ApplyCircuitIdCookie(HttpContext context, CircuitId circuitId)
        {
            var id = circuitId.RequestToken;
            var cookieToken = circuitId.CookieToken;
            var cookieName = $"{CookiePrefix}{GetCircuitIdPrefix(id)}";

            var options = CreateCookieOptions(context, cookieName);
            context.Response.Cookies.Append(cookieName, cookieToken, options);

            // We create a temporary keep alive cookie to preserve the circuit cookie while this companion
            // cookie is present. This is due to the fact that we only register the circuit as disconnected
            // after we know for sure the response completed without navigating away (for example redirecting).
            var keepAliveCookieName = GetKeepAliveCookieName(cookieName);
            var keepAliveOptions = CreateCookieOptions(context, keepAliveCookieName, TimeSpan.FromMinutes(5));
            context.Response.Cookies.Append(keepAliveCookieName, "", keepAliveOptions);

            return Task.CompletedTask;
        }


        private static string GetKeepAliveCookieName(string cookieName) => $"{cookieName}.KeepAlive";

        private static CookieOptions CreateCookieOptions(HttpContext context, string cookieName, TimeSpan? maxAge = null)
        {
            // We don't want to expose options for this cookie to users as we want to keep it as much of an implementation
            // detail as possible. We might need to consider if users need to be able to change this.
            // At least the name.
            var builder = new CookieBuilder()
            {
                HttpOnly = true,
                Name = cookieName,
                SameSite = Http.SameSiteMode.Strict,
                SecurePolicy = CookieSecurePolicy.SameAsRequest,
                IsEssential = true,
                MaxAge = maxAge,
                Expiration = maxAge
            };

            return builder.Build(context);
        }

        internal static Task AttachCircuitIdAsync(
            HttpContext httpContext,
            CircuitId circuitId)
        {
            SetResponseHeaders(httpContext.Response);
            return ApplyCircuitIdCookie(httpContext, circuitId);
        }

        private static void SetResponseHeaders(HttpResponse response)
        {
            response.Headers[HeaderNames.CacheControl] = "no-cache, no-store";
            response.Headers[HeaderNames.Pragma] = "no-cache";
        }

        private static string GetCircuitIdPrefix(StringValues circuitId) =>
            ((string)circuitId).Substring(0, PrefixLenght);
    }
}
