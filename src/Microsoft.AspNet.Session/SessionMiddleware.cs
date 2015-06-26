// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Http.Features;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Session
{
    public class SessionMiddleware
    {
        private static readonly RandomNumberGenerator CryptoRandom = RandomNumberGenerator.Create();
        private const int SessionKeyLength = 36; // "382c74c3-721d-4f34-80e5-57657b6cbc27"
        private static readonly Func<bool> ReturnTrue = () => true;
        private readonly RequestDelegate _next;
        private readonly SessionOptions _options;
        private readonly ILogger _logger;
        private readonly ISessionStore _sessionStore;

        public SessionMiddleware(
            [NotNull] RequestDelegate next,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] ISessionStore sessionStore,
            [NotNull] IOptions<SessionOptions> options)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<SessionMiddleware>();
            _options = options.Options;
            _sessionStore = sessionStore;
            _sessionStore.Connect();
        }

        public async Task Invoke(HttpContext context)
        {
            var isNewSessionKey = false;
            Func<bool> tryEstablishSession = ReturnTrue;
            var sessionKey = context.Request.Cookies.Get(_options.CookieName);
            if (string.IsNullOrWhiteSpace(sessionKey) || sessionKey.Length != SessionKeyLength)
            {
                // No valid cookie, new session.
                var guidBytes = new byte[16];
                CryptoRandom.GetBytes(guidBytes);
                sessionKey = new Guid(guidBytes).ToString();
                var establisher = new SessionEstablisher(context, sessionKey, _options);
                tryEstablishSession = establisher.TryEstablishSession;
                isNewSessionKey = true;
            }

            var feature = new SessionFeature();
            feature.Session = _sessionStore.Create(sessionKey, _options.IdleTimeout, tryEstablishSession, isNewSessionKey);
            context.SetFeature<ISessionFeature>(feature);

            try
            {
                await _next(context);
            }
            finally
            {
                context.SetFeature<ISessionFeature>(null);

                if (feature.Session != null)
                {
                    try
                    {
                        await feature.Session.CommitAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("Error closing the session.", ex);
                    }
                }
            }
        }

        private class SessionEstablisher
        {
            private readonly HttpContext _context;
            private readonly string _sessionKey;
            private readonly SessionOptions _options;
            private bool _shouldEstablishSession;

            public SessionEstablisher(HttpContext context, string sessionKey, SessionOptions options)
            {
                _context = context;
                _sessionKey = sessionKey;
                _options = options;
                context.Response.OnStarting(OnStartingCallback, state: this);
            }

            private static Task OnStartingCallback(object state)
            {
                var establisher = (SessionEstablisher)state;
                if (establisher._shouldEstablishSession)
                {
                    establisher.SetCookie();
                }
                return Task.FromResult(0);
            }

            private void SetCookie()
            {
                var cookieOptions = new CookieOptions
                {
                    Domain = _options.CookieDomain,
                    HttpOnly = _options.CookieHttpOnly,
                    Path = _options.CookiePath ?? SessionDefaults.CookiePath,
                };

                _context.Response.Cookies.Append(_options.CookieName, _sessionKey, cookieOptions);

                _context.Response.Headers.Set(
                    "Cache-Control",
                    "no-cache");

                _context.Response.Headers.Set(
                    "Pragma",
                    "no-cache");

                _context.Response.Headers.Set(
                    "Expires",
                    "-1");
            }

            // Returns true if the session has already been established, or if it still can be because the response has not been sent.
            internal bool TryEstablishSession()
            {
                return (_shouldEstablishSession |= !_context.Response.HasStarted);
            }
        }
    }
}