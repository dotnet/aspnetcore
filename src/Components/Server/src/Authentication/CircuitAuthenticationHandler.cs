// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Server
{
    internal class CircuitAuthenticationHandler : SignInAuthenticationHandler<CircuitAuthenticationOptions>
    {
        internal const string IdClaimType = "bcid";
        internal const string AuthenticationType = "Circuit";
        private const int PrefixLenght = 4;

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
            if (!Context.Request.Query.TryGetValue("CircuitId", out var circuitId) || circuitId.Count != 1)
            {
                // There is no circuit id on the query string, so we don't authenticate anything.
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var key = $"Circuit.{GetCircuitIdPrefix(circuitId)}";

            if (Context.Request.Cookies.TryGetValue(key, out var cookie))
            {
                if (CircuitIdFactory.ValidateCircuitId(circuitId, cookie, out var parsedId))
                {
                    var identity = new ClaimsIdentity(AuthenticationType);
                    identity.AddClaim(new Claim(IdClaimType, parsedId.RequestToken));
                    var principal = new ClaimsPrincipal();
                    principal.AddIdentity(identity);
                    return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, AuthenticationType)));
                }
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }

        private static string GetCircuitIdPrefix(StringValues circuitId)
        {
            return ((string)circuitId).Substring(0, PrefixLenght);
        }

        protected override Task HandleSignInAsync(ClaimsPrincipal user, AuthenticationProperties properties)
        {
            var id = properties.Items["RequestToken"];
            var cookieToken = properties.Items["CookieToken"];
            var cookieName = $"Circuit.{GetCircuitIdPrefix(id)}";

            var options = new CookieBuilder()
            {
                HttpOnly = true,
                Name = cookieName,
                SameSite = SameSiteMode.Strict,
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

        private string GetId(string value)
        {
            try
            {
                CircuitId result = CircuitIdFactory.FromCookieValue(value);
                return result.RequestToken;
            }
            catch
            {
                // Cookie didn't have a valid format, treating them as if it didn't exist.
                // We might want to log here that something went wrong.
                return null;
            }
        }

        internal static void AttachCircuitId(HttpContext httpContext, CircuitId circuitId)
        {
            var principal = new ClaimsPrincipal();
            var identity = new ClaimsIdentity(AuthenticationType);
            principal.AddIdentity(identity);

            var properties = new AuthenticationProperties();
            properties.Items["RequestToken"] = circuitId.RequestToken;
            properties.Items["CookieToken"] = circuitId.CookieToken;

            httpContext.SignInAsync(
                AuthenticationType,
                principal,
                properties);
        }
    }
}
