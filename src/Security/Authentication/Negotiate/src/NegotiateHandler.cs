// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    /// <summary>
    /// Authenticates requests using Negotiate, Kerberos, or NTLM.
    /// </summary>
    public class NegotiateHandler : AuthenticationHandler<NegotiateOptions>, IAuthenticationRequestHandler
    {
        private const string NegotiateStateKey = nameof(INegotiateState);
        private const string NegotiateVerb = "Negotiate";
        private const string AuthHeaderPrefix = NegotiateVerb + " ";

        /// <summary>
        /// Creates a new <see cref="NegotiateHandler"/>
        /// </summary>
        /// <param name="options"></param>
        /// <param name="logger"></param>
        /// <param name="encoder"></param>
        /// <param name="clock"></param>
        public NegotiateHandler(IOptionsMonitor<NegotiateOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        { }

        /// <summary>
        /// The handler calls methods on the events which give the application control at certain points where processing is occurring. 
        /// If it is not provided a default instance is supplied which does nothing when the methods are called.
        /// </summary>
        protected new NegotiateEvents Events
        {
            get => (NegotiateEvents)base.Events;
            set => base.Events = value;
        }

        /// <summary>
        /// Creates the default events type.
        /// </summary>
        /// <returns></returns>
        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new NegotiateEvents());

        private bool IsHttp2 => string.Equals("HTTP/2", Request.Protocol, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Intercepts incomplete Negotiate authentication handshakes and continues or completes them.
        /// </summary>
        /// <returns>True if a response was generated, false otherwise.</returns>
        public async Task<bool> HandleRequestAsync()
        {
            try
            {
                if (IsHttp2)
                {
                    // HTTP/2 is not supported. Do not throw because this may be running on a server that supports
                    // both HTTP/1 and HTTP/2.
                    return false;
                }

                var connectionItems = GetConnectionItems();
                var authState = (INegotiateState)connectionItems[NegotiateStateKey];

                var authorizationHeader = Request.Headers[HeaderNames.Authorization];

                if (StringValues.IsNullOrEmpty(authorizationHeader))
                {
                    if (authState?.IsCompleted == false)
                    {
                        throw new InvalidOperationException("An anonymous request was received in between authentication handshake requests.");
                    }
                    Logger.LogDebug($"No Authorization header");
                    return false;
                }

                var authorization = authorizationHeader.ToString();
                Logger.LogTrace($"Authorization: " + authorization);
                string token = null;
                if (authorization.StartsWith(AuthHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    token = authorization.Substring(AuthHeaderPrefix.Length).Trim();
                }
                else
                {
                    Logger.LogDebug($"Non-Negotiate Authorization header");
                    return false;
                }

                if (authState == null)
                {
                    connectionItems[NegotiateStateKey] = authState = Options.StateFactory.CreateInstance();
                    RegisterForConnectionDispose(authState);
                }

                var outgoing = authState.GetOutgoingBlob(token);
                if (!authState.IsCompleted)
                {
                    Logger.LogInformation("Incomplete Negotiate, 401 Negotiate challenge");
                    Response.StatusCode = StatusCodes.Status401Unauthorized;
                    Response.Headers.Append(HeaderNames.WWWAuthenticate, AuthHeaderPrefix + outgoing);
                    return true;
                }

                // TODO SPN check? NTLM + CBT only?

                // TODO: Consider disposing the authState and caching a copy of the user instead. You would need to clone that user per-request
                // to avoid contaimination from claims transformation.

                Logger.LogDebug($"Completed Negotiate, Negotiate");
                if (!string.IsNullOrEmpty(outgoing))
                {
                    // There can be a final blob of data we need to send to the client, but let the request execute as normal.
                    Response.Headers.Append(HeaderNames.WWWAuthenticate, AuthHeaderPrefix + outgoing);
                }

                // Note we run the Authenticated event in HandleAuthenticateAsync so it is per-request rather than per connection.
            }
            catch (Exception ex)
            {
                var errorContext = new AuthenticationFailedContext(Context, Scheme, Options) { Exception = ex };
                await Events.AuthenticationFailed(errorContext);

                if (errorContext.Result != null)
                {
                    if (errorContext.Result.Handled)
                    {
                        return true;
                    }
                    else if (errorContext.Result.Skipped)
                    {
                        return false;
                    }
                    else if (errorContext.Result.Failure != null)
                    {
                        throw new Exception("An error was returned from the AuthenticationFailed event.", errorContext.Result.Failure);
                    }
                }

                throw;
            }

            return false;
        }

        /// <summary>
        /// Checks if the current request is authenticated and returns the user.
        /// </summary>
        /// <returns></returns>
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (IsHttp2)
            {
                // Not supported. We don't throw because Negotiate may be set as the default auth
                // handler on a server that's running HTTP/1 and HTTP/2.
                return AuthenticateResult.NoResult();
            }

            var authState = (INegotiateState)GetConnectionItems()[NegotiateStateKey];
            if (authState != null && authState.IsCompleted)
            {
                Logger.LogDebug($"Cached User");

                // Make a new copy of the user for each request, they are mutable objects and
                // things like ClaimsTransformation run per request.
                var identity = authState.GetIdentity();
                ClaimsPrincipal user;
                if (identity is WindowsIdentity winIdentity)
                {
                    user = new WindowsPrincipal(winIdentity);
                    Response.RegisterForDispose(winIdentity);
                }
                else
                {
                    user = new ClaimsPrincipal(new ClaimsIdentity(identity));
                }

                var authenticatedContext = new AuthenticatedContext(Context, Scheme, Options)
                {
                    Principal = user
                };
                await Events.OnAuthenticated(authenticatedContext);

                if (authenticatedContext.Result != null)
                {
                    return authenticatedContext.Result;
                }

                var ticket = new AuthenticationTicket(user, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }

            return AuthenticateResult.NoResult();
        }

        /// <summary>
        /// Issues a 401 WWW-Authenticate Negotiate challenge.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // TODO: Verify clients will downgrade from HTTP/2 to HTTP/1?
            // Or do we need to send HTTP_1_1_REQUIRED? Or throw here?
            // TODO: Should we invalidate your current auth state?
            var authResult = await HandleAuthenticateOnceSafeAsync();
            var eventContext = new ChallengeContext(Context, Scheme, Options, properties)
            {
                AuthenticateFailure = authResult?.Failure
            };

            await Events.Challenge(eventContext);
            if (eventContext.Handled)
            {
                return;
            }

            Logger.LogDebug($"Challenged 401 Negotiate");
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers.Append(HeaderNames.WWWAuthenticate, NegotiateVerb);
        }

        private IDictionary<object, object> GetConnectionItems()
        {
            var connectionItems = Context.Features.Get<IConnectionItemsFeature>()?.Items;
            if (connectionItems == null)
            {
                throw new NotSupportedException($"Negotiate authentication requires a server that supports {nameof(IConnectionItemsFeature)} like Kestrel.");
            }

            return connectionItems;
        }

        private void RegisterForConnectionDispose(INegotiateState authState)
        {
            var connectionCompleteFeature = Context.Features.Get<IConnectionCompleteFeature>();
            if (connectionCompleteFeature == null)
            {
                throw new NotSupportedException($"Negotiate authentication requires a server that supports {nameof(IConnectionCompleteFeature)} like Kestrel.");
            }
            connectionCompleteFeature.OnCompleted(DisposeState, authState);
        }

        private static Task DisposeState(object state)
        {
            ((IDisposable)state).Dispose();
            return Task.CompletedTask;
        }
    }
}
