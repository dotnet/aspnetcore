// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Authentication.Negotiate
{
    public class NegotiateHandler : AuthenticationHandler<NegotiateOptions>, IAuthenticationRequestHandler
    {
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

        protected override Task<object> CreateEventsAsync() => Task.FromResult<object>(new NegotiateEvents());

        // TODO: Move to server connection storage feature.
        // This has no cleaup. Static because handlers are per-request.
        // These instances should be disposed when all requests are complete and the connection is cleaned up.
        private static ConcurrentDictionary<string, INegotiateState> _states = new ConcurrentDictionary<string, INegotiateState>();

        private string _verb = "Negotiate";// "NTLM";

        // Intercept incomplete auth handshakes and continue or complete them.
        public Task<bool> HandleRequestAsync()
        {
            var connectionId = Context.Connection.Id;
            var authorization = Request.Headers[HeaderNames.Authorization].ToString();

            if (string.IsNullOrEmpty(authorization))
            {
                Logger.LogDebug($"C:{connectionId}, No Authorization header");
                return Task.FromResult(false);
            }

            Logger.LogDebug($"C:{connectionId}, Authorization: " + authorization);
            string token = null;
            if (authorization.StartsWith($"{_verb} ", StringComparison.OrdinalIgnoreCase))
            {
                token = authorization.Substring($"{_verb} ".Length).Trim();
            }

            // If no token found, no further work possible
            if (string.IsNullOrEmpty(token))
            {
                Logger.LogDebug($"C:{connectionId}, Non-Negotiate Authorization header");
                return Task.FromResult(false);
            }

            var authState = _states.GetOrAdd(connectionId, _ => Options.StateFactory.CreateInstance());
            var outgoing = authState.GetOutgoingBlob(token);
            if (!authState.IsCompleted)
            {
                Logger.LogInformation($"C:{connectionId}, Incomplete-Negotiate, 401 {_verb} {outgoing}");
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                Response.Headers.Append(HeaderNames.WWWAuthenticate, $"{_verb} {outgoing}");
                return Task.FromResult(true);
            }

            // TODO SPN check?

            Logger.LogInformation($"C:{connectionId}, Completed-Negotiate, {_verb} {outgoing}");
            if (!string.IsNullOrEmpty(outgoing))
            {
                // There can be a final blob of data we need to send to the client, but let the request execute as normal.
                Response.Headers.Append(HeaderNames.WWWAuthenticate, $"{_verb} {outgoing}");
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Checks if the current connection is authenticated and returns the user.
        /// </summary>
        /// <returns></returns>
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var connectionId = Context.Connection.Id;
            if (_states.TryGetValue(connectionId, out var auth) && auth.IsCompleted)
            {
                Logger.LogInformation($"C:{connectionId}, Cached User");
                var user = auth.GetPrincipal();
                var ticket = new AuthenticationTicket(user, Scheme.Name);
                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
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

            Response.StatusCode = StatusCodes.Status401Unauthorized;
            Response.Headers.Append(HeaderNames.WWWAuthenticate, _verb);
        }
    }
}
