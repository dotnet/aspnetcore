// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private const string AuthPersistenceKey = nameof(AuthPersistence);
        private const string NegotiateVerb = "Negotiate";
        private const string AuthHeaderPrefix = NegotiateVerb + " ";

        private bool _requestProcessed;
        private INegotiateState _negotiateState;

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
                if (_requestProcessed)
                {
                    // This request was already processed but something is re-executing it like an exception handler.
                    // Don't re-run because we could corrupt the connection state, e.g. if this was a stage2 NTLM request
                    // that we've already completed the handshake for.
                    return false;
                }

                _requestProcessed = true;

                if (IsHttp2)
                {
                    // HTTP/2 is not supported. Do not throw because this may be running on a server that supports
                    // both HTTP/1 and HTTP/2.
                    return false;
                }

                var connectionItems = GetConnectionItems();
                var persistence = (AuthPersistence)connectionItems[AuthPersistenceKey];
                _negotiateState = persistence?.State;

                var authorizationHeader = Request.Headers[HeaderNames.Authorization];

                if (StringValues.IsNullOrEmpty(authorizationHeader))
                {
                    if (_negotiateState?.IsCompleted == false)
                    {
                        throw new InvalidOperationException("An anonymous request was received in between authentication handshake requests.");
                    }
                    Logger.LogDebug($"No Authorization header.");
                    return false;
                }

                var authorization = authorizationHeader.ToString();
                string token = null;
                if (authorization.StartsWith(AuthHeaderPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    token = authorization.Substring(AuthHeaderPrefix.Length).Trim();
                }
                else
                {
                    if (_negotiateState?.IsCompleted == false)
                    {
                        throw new InvalidOperationException("Non-negotiate request was received in between authentication handshake requests.");
                    }
                    Logger.LogDebug($"Non-Negotiate Authorization header.");
                    return false;
                }

                // WinHttpHandler re-authenticates an existing connection if it gets another challenge on subsequent requests.
                if (_negotiateState?.IsCompleted == true)
                {
                    Logger.LogDebug("Negotiate data received for an already authenticated connection, Re-authenticating.");
                    _negotiateState.Dispose();
                    _negotiateState = null;
                    persistence.State = null;
                }

                _negotiateState ??= Options.StateFactory.CreateInstance();

                var outgoing = _negotiateState.GetOutgoingBlob(token);

                if (!_negotiateState.IsCompleted)
                {
                    Debug.Assert(_negotiateState.Protocol == "NTLM", "Only NTLM should require multiple stages.");

                    Logger.LogDebug($"Enabling credential persistence for an incomplete NTLM handshake.");
                    persistence ??= EstablishConnectionPersistence(connectionItems);
                    // Save the state long enough to complete the multi-stage handshake.
                    // We'll remove it once complete if !PersistNtlmCredentials.
                    persistence.State = _negotiateState;

                    Logger.LogInformation("Incomplete Negotiate, sending a second 401 Negotiate challenge.");
                    Response.StatusCode = StatusCodes.Status401Unauthorized;
                    Response.Headers.Append(HeaderNames.WWWAuthenticate, AuthHeaderPrefix + outgoing);
                    return true;
                }

                Logger.LogDebug($"Completed Negotiate.");

                // Isn't there always additional data for the server scenario?
                if (!string.IsNullOrEmpty(outgoing))
                {
                    // There can be a final blob of data we need to send to the client, but let the request execute as normal.
                    Response.Headers.Append(HeaderNames.WWWAuthenticate, AuthHeaderPrefix + outgoing);
                }

                // Deal with connection credential persistence.

                if (_negotiateState.Protocol == "NTLM" && !Options.PersistNtlmCredentials)
                {
                    // NTLM was already put in the persitence cache on the prior request so we could complete the handshake.
                    // Take it out if we don't want it to persist.
                    Debug.Assert(object.ReferenceEquals(persistence?.State, _negotiateState),
                        "NTLM is a two stage process, it must have already been in the cache for the handshake to succeed.");
                    Logger.LogDebug($"Disabling credential persistence for a complete NTLM handshake.");
                    persistence.State = null;
                    Response.RegisterForDispose(_negotiateState);
                }
                else if (_negotiateState.Protocol == "Kerberos" && Options.PersistKerberosCredentials)
                {
                    Logger.LogDebug($"Enabling credential persistence for a complete Kerberos handshake.");
                    persistence ??= EstablishConnectionPersistence(connectionItems);
                    Debug.Assert(persistence.State == null, "Complete Kerberos results should only be produced from a new context.");
                    persistence.State = _negotiateState;
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
            if (!_requestProcessed)
            {
                throw new InvalidOperationException("AuthenticateAsync must not be called before the UseAuthentication middleware runs.");
            }

            if (IsHttp2)
            {
                // Not supported. We don't throw because Negotiate may be set as the default auth
                // handler on a server that's running HTTP/1 and HTTP/2. We'll challenge HTTP/2 requests
                // that require auth and they'll downgrade to HTTP/1.1.
                return AuthenticateResult.NoResult();
            }

            if (_negotiateState == null)
            {
                return AuthenticateResult.NoResult();
            }

            if (!_negotiateState.IsCompleted)
            {
                // This case should have been rejected by HandleRequestAsync
                throw new InvalidOperationException("Attempting to use an incomplete authentication context.");
            }

            // Make a new copy of the user for each request, they are mutable objects and
            // things like ClaimsTransformation run per request.
            var identity = _negotiateState.GetIdentity();
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
            await Events.Authenticated(authenticatedContext);

            if (authenticatedContext.Result != null)
            {
                return authenticatedContext.Result;
            }

            var ticket = new AuthenticationTicket(authenticatedContext.Principal, authenticatedContext.Properties, Scheme.Name);
            return AuthenticateResult.Success(ticket);
        }

        /// <summary>
        /// Issues a 401 WWW-Authenticate Negotiate challenge.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // We allow issuing a challenge from an HTTP/2 request. Browser clients will gracefully downgrade to HTTP/1.1.
            // SocketHttpHandler will not downgrade (https://github.com/dotnet/corefx/issues/35195), but WinHttpHandler will.
            var eventContext = new ChallengeContext(Context, Scheme, Options, properties);
            await Events.Challenge(eventContext);
            if (eventContext.Handled)
            {
                return;
            }

            Logger.LogDebug($"Challenged 401 Negotiate");
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers.Append(HeaderNames.WWWAuthenticate, NegotiateVerb);
        }

        private AuthPersistence EstablishConnectionPersistence(IDictionary<object, object> items)
        {
            Debug.Assert(!items.ContainsKey(AuthPersistenceKey), "This should only be registered once per connection");
            var persistence = new AuthPersistence();
            RegisterForConnectionDispose(persistence);
            items[AuthPersistenceKey] = persistence;
            return persistence;
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

        private void RegisterForConnectionDispose(IDisposable authState)
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

        // This allows us to have one disposal registration per connection and limits churn on the Items collection.
        private class AuthPersistence : IDisposable
        {
            internal INegotiateState State { get; set; }

            public void Dispose()
            {
                State?.Dispose();
            }
        }
    }
}
