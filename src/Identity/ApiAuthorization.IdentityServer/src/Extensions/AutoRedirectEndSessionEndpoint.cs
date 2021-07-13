// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Specialized;
using System.Net;
using System.Threading.Tasks;
using Duende.IdentityServer.Configuration;
using Duende.IdentityServer.Endpoints.Results;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Hosting;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Validation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    internal class AutoRedirectEndSessionEndpoint : IEndpointHandler
    {
        private readonly ILogger _logger;
        private readonly IUserSession _session;
        private readonly IOptions<IdentityServerOptions> _identityServerOptions;
        private readonly IEndSessionRequestValidator _requestvalidator;

        public AutoRedirectEndSessionEndpoint(
            ILogger<AutoRedirectEndSessionEndpoint> logger,
            IEndSessionRequestValidator requestValidator,
            IOptions<IdentityServerOptions> identityServerOptions,
            IUserSession session)
        {
            _logger = logger;
            _session = session;
            _identityServerOptions = identityServerOptions;
            _requestvalidator = requestValidator;
        }

        public async Task<IEndpointResult> ProcessAsync(HttpContext ctx)
        {
            var validtionResult = ValidateRequest(ctx.Request);
            if (validtionResult != null)
            {
                return validtionResult;
            }

            var parameters = await GetParametersAsync(ctx.Request);
            var user = await _session.GetUserAsync();
            var result = await _requestvalidator.ValidateAsync(parameters, user);
            if (result.IsError)
            {
                _logger.LogError(LoggerEventIds.EndingSessionFailed, "Error ending session {Error}", result.Error);
                return new RedirectResult(_identityServerOptions.Value.UserInteraction.ErrorUrl);
            }

            var client = result.ValidatedRequest?.Client;
            if (client != null &&
                client.Properties.TryGetValue(ApplicationProfilesPropertyNames.Profile, out var type))
            {
                var signInScheme = _identityServerOptions.Value.Authentication.CookieAuthenticationScheme;
                if (signInScheme != null)
                {
                    await ctx.SignOutAsync(signInScheme);
                }
                else
                {
                    await ctx.SignOutAsync();
                }

                var postLogOutUri = result.ValidatedRequest.PostLogOutUri;
                if (result.ValidatedRequest.State != null)
                {
                    postLogOutUri = QueryHelpers.AddQueryString(postLogOutUri, OpenIdConnectParameterNames.State, result.ValidatedRequest.State);
                }

                return new RedirectResult(postLogOutUri);
            }
            else
            {
                return new RedirectResult(_identityServerOptions.Value.UserInteraction.LogoutUrl);
            }
        }

        private async Task<NameValueCollection> GetParametersAsync(HttpRequest request)
        {
            if (HttpMethods.IsGet(request.Method))
            {
                return request.Query.AsNameValueCollection();
            }
            else
            {
                var form = await request.ReadFormAsync();
                return form.AsNameValueCollection();
            }
        }

        private IEndpointResult ValidateRequest(HttpRequest request)
        {
            if (!HttpMethods.IsPost(request.Method) && !HttpMethods.IsGet(request.Method))
            {
                return new StatusCodeResult(HttpStatusCode.BadRequest);
            }

            if (HttpMethods.IsPost(request.Method) &&
                !string.Equals(request.ContentType, "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
            {
                return new StatusCodeResult(HttpStatusCode.BadRequest);
            }

            return null;
        }

        internal class RedirectResult : IEndpointResult
        {

            public RedirectResult(string url)
            {
                Url = url;
            }

            public string Url { get; }

            public Task ExecuteAsync(HttpContext context)
            {
                context.Response.Redirect(Url);
                return Task.CompletedTask;
            }
        }
    }
}
