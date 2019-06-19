// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Components.Server
{
    // Authentication handler for circuit ids.
    // We pair each circuit id with a cookie.
    // We use an authentication handler to enable smooth integration with signalr service, as it preserves limited
    // information about the connection context (amongs it the Principal), that's why we do it that way.
    internal class CircuitAuthenticationHandler : SignInAuthenticationHandler<CircuitAuthenticationOptions>
    {
        // Stands for Blazor Circuit ID
        // Claim types are supposed to be kept short.
        internal const string IdClaimType = "bcid";
        internal const string AuthenticationType = "Circuit";
        internal const string AuthorizationPolicyName = "Circuit";
        internal const string CookiePrefix = "Circuit.";

        private const int PrefixLenght = 4;
        private const string RequestTokenKey = "RequestToken";
        private const string CookieTokenKey = "CookieToken";
        private const string QueryStringParameterKey = "CircuitId";

        public CircuitAuthenticationHandler(
            IOptionsMonitor<CircuitAuthenticationOptions> options,
            CircuitIdFactory circuitIdFactory,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
            CircuitIdFactory = circuitIdFactory;
        }

        public CircuitIdFactory CircuitIdFactory { get; }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Context.Request.Query.TryGetValue(QueryStringParameterKey, out var circuitId) || circuitId.Count != 1)
            {
                // There is no circuit id on the query string, so we don't authenticate anything.
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var key = $"{CookiePrefix}{GetCircuitIdPrefix(circuitId)}";

            if (Context.Request.Cookies.TryGetValue(key, out var cookie))
            {
                if (CircuitIdFactory.ValidateCircuitId(circuitId, cookie, out var parsedId))
                {
                    // We create an identity without authentication type so that the presence of this
                    // identity doesn't affect ClaimsPrincipal.IsAuthenticated
                    var principal = new ClaimsPrincipal();

                    var identity = new ClaimsIdentity();
                    identity.AddClaim(new Claim(IdClaimType, parsedId.RequestToken));
                    principal.AddIdentity(identity);

                    return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, AuthenticationType)));
                }
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }

        protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
        {
            var id = properties.Items[RequestTokenKey];
            var cookieToken = properties.Items[CookieTokenKey];
            var cookieName = $"{CookiePrefix}{GetCircuitIdPrefix(id)}";

            // We don't want to expose options for this cookie to users as we want to keep it as much of an implementation
            // detail as possible. We might need to consider if users need to be able to change this.
            // At least the name.
            var options = new CookieBuilder()
            {
                HttpOnly = true,
                Name = cookieName,
                SameSite = Http.SameSiteMode.Strict,
                SecurePolicy = CookieSecurePolicy.SameAsRequest,
                IsEssential = true
            }.Build(Context);

            Context.Response.Cookies.Append(cookieName, cookieToken, options);
            return Task.CompletedTask;
        }

        protected override Task HandleSignOutAsync(AuthenticationProperties properties)
        {
            throw new InvalidOperationException("Sign out is not supported.");
        }

        internal static async Task AttachCircuitIdAsync(HttpContext httpContext, CircuitId circuitId)
        {
            // We require to pass in an authenticated principal to SignInAsync, but we are
            // simply going to ignore it inside HandleSignInAsync and use the authentication
            // properties to setup the cookie in the request.
            // This is because what we are authenticating here is the connection and not a specific user.
            // We might in the future attach some information associated with the user to the connection
            // so that you also need to be in possesion of the auth cookie to connect to the circuit.
            var principal = new ClaimsPrincipal();
            var identity = new ClaimsIdentity(AuthenticationType);
            principal.AddIdentity(identity);

            var properties = new AuthenticationProperties();
            properties.Items[RequestTokenKey] = circuitId.RequestToken;
            properties.Items[CookieTokenKey] = circuitId.CookieToken;

            SetResponseHeaders(httpContext.Response);

            await httpContext.SignInAsync(
                AuthenticationType,
                principal,
                properties);
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
