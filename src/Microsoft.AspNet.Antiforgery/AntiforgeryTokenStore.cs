// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Antiforgery
{
    // Saves anti-XSRF tokens split between HttpRequest.Cookies and HttpRequest.Form
    public sealed class AntiforgeryTokenStore : IAntiforgeryTokenStore
    {
        private readonly AntiforgeryOptions _config;
        private readonly IAntiforgeryTokenSerializer _serializer;

        public AntiforgeryTokenStore([NotNull] AntiforgeryOptions config,
                                       [NotNull] IAntiforgeryTokenSerializer serializer)
        {
            _config = config;
            _serializer = serializer;
        }

        public AntiforgeryToken GetCookieToken(HttpContext httpContext)
        {
            var contextAccessor =
                httpContext.RequestServices.GetRequiredService<IAntiforgeryContextAccessor>();
            if (contextAccessor.Value != null)
            {
                return contextAccessor.Value.CookieToken;
            }

            var requestCookie = httpContext.Request.Cookies[_config.CookieName];
            if (string.IsNullOrEmpty(requestCookie))
            {
                // unable to find the cookie.
                return null;
            }

            return _serializer.Deserialize(requestCookie);
        }

        public async Task<AntiforgeryToken> GetFormTokenAsync(HttpContext httpContext)
        {
            var form = await httpContext.Request.ReadFormAsync();
            var value = form[_config.FormFieldName];
            if (string.IsNullOrEmpty(value))
            {
                // did not exist
                return null;
            }

            return _serializer.Deserialize(value);
        }

        public void SaveCookieToken(HttpContext httpContext, AntiforgeryToken token)
        {
            // Add the cookie to the request based context.
            // This is useful if the cookie needs to be reloaded in the context of the same request.
            var contextAccessor =
                httpContext.RequestServices.GetRequiredService<IAntiforgeryContextAccessor>();
            Debug.Assert(contextAccessor.Value == null, "AntiforgeryContext should be set only once per request.");
            contextAccessor.Value = new AntiforgeryContext() { CookieToken = token };

            var serializedToken = _serializer.Serialize(token);
            var options = new CookieOptions() { HttpOnly = true };

            // Note: don't use "newCookie.Secure = _config.RequireSSL;" since the default
            // value of newCookie.Secure is poulated out of band.
            if (_config.RequireSSL)
            {
                options.Secure = true;
            }

            httpContext.Response.Cookies.Append(_config.CookieName, serializedToken, options);
        }
    }
}