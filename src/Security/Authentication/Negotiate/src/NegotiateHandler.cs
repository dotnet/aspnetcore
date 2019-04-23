// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
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
        private static string NegotiateStateKey = nameof(INegotiateState);
        // TODO: CONFIG?
        private static string _verb = "Negotiate";// "NTLM";
        private static string _prefix = _verb + " ";

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

        /// <summary>
        /// Intercepts incomplete auth handshakes and continues or completes them.
        /// </summary>
        /// <returns>True if a response was generated, false otherwise.</returns>
        public async Task<bool> HandleRequestAsync()
        {
            try
            {
                var connectionId = Context.Connection.Id;
                var authorizationHeader = Request.Headers[HeaderNames.Authorization];

                if (StringValues.IsNullOrEmpty(authorizationHeader))
                {
                    Logger.LogDebug($"C:{connectionId}, No Authorization header");
                    return false;
                }

                var authorization = authorizationHeader.ToString();
                Logger.LogTrace($"C:{connectionId}, Authorization: " + authorization);
                string token = null;
                if (authorization.StartsWith(_prefix, StringComparison.OrdinalIgnoreCase))
                {
                    token = authorization.Substring(_prefix.Length).Trim();
                }

                // If no token found, no further work possible
                if (string.IsNullOrEmpty(token))
                {
                    Logger.LogDebug($"C:{connectionId}, Non-Negotiate Authorization header");
                    return false;
                }

                var connectionItems = GetConnectionItems();

                var authState = (INegotiateState)connectionItems[NegotiateStateKey];
                if (authState == null)
                {
                    // TODO: IConnectionLifetimeFeature would fire mid-request.
                    // Replace with a connetion level version of Response.RegisterForDispose.
                    var connectionLifetimeFeature = Context.Features.Get<IConnectionLifetimeFeature>();
                    if (connectionLifetimeFeature == null || !connectionLifetimeFeature.ConnectionClosed.CanBeCanceled)
                    {
                        throw new NotSupportedException($"Negotiate authentication requires a server that supports {nameof(IConnectionLifetimeFeature)}");
                    }
                    connectionItems[NegotiateStateKey] = authState = Options.StateFactory.CreateInstance();
                    connectionLifetimeFeature.ConnectionClosed.UnsafeRegister(DisposeState, authState);
                }

                var outgoing = authState.GetOutgoingBlob(token);
                if (!authState.IsCompleted)
                {
                    Logger.LogInformation($"C:{connectionId}, Incomplete-Negotiate, 401 {_verb} {outgoing}");
                    Response.StatusCode = StatusCodes.Status401Unauthorized;
                    Response.Headers.Append(HeaderNames.WWWAuthenticate, $"{_verb} {outgoing}");

                    // TODO: Consider disposing the authState and caching a copy of the user instead.
                    return true;
                }

                // TODO SPN check? NTLM + CBT only?

                Logger.LogInformation($"C:{connectionId}, Completed-Negotiate, {_verb} {outgoing}");
                if (!string.IsNullOrEmpty(outgoing))
                {
                    // There can be a final blob of data we need to send to the client, but let the request execute as normal.
                    Response.Headers.Append(HeaderNames.WWWAuthenticate, $"{_verb} {outgoing}");
                }
            }
            catch (Exception ex)
            {
                var context = new AuthenticationFailedContext(Context, Scheme, Options) { Exception = ex };
                await Events.AuthenticationFailed(context);
                // TODO: Handled, return true/false or rethrow.
                throw;
            }

            return false;
        }

        /// <summary>
        /// Checks if the current request is authenticated and returns the user.
        /// </summary>
        /// <returns></returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authState = (INegotiateState)GetConnectionItems()[NegotiateStateKey];
            var connectionId = Context.Connection.Id;
            if (authState != null && authState.IsCompleted)
            {
                Logger.LogDebug($"C:{connectionId}, Cached User");

                // Make a new copy of the user for each request, they are mutable objects
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

                var ticket = new AuthenticationTicket(user, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }

        /// <summary>
        /// Issues a 401 WWW-Authenticate challenge.
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            // TODO: Should we invalidate your current auth state?
            var authResult = await HandleAuthenticateOnceSafeAsync();
            var eventContext = new NegotiateChallengeContext(Context, Scheme, Options, properties)
            {
                AuthenticateFailure = authResult?.Failure
            };

            await Events.Challenge(eventContext);
            if (eventContext.Handled)
            {
                return;
            }

            var connectionId = Context.Connection.Id;
            Logger.LogDebug($"C:{connectionId}, Challenged");
            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers.Append(HeaderNames.WWWAuthenticate, _verb);
        }

        private IDictionary<object, object> GetConnectionItems()
        {
            var connectionItems = Context.Features.Get<IConnectionItemsFeature>()?.Items;
            if (connectionItems == null)
            {
                throw new NotSupportedException($"Negotiate authentication requires a server that supports {nameof(IConnectionItemsFeature)}");
            }

            return connectionItems;
        }

        private static void DisposeState(object state)
        {
            ((IDisposable)state).Dispose();
        }
    }
}
