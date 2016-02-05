// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Antiforgery.Internal
{
    public class DefaultAntiforgeryTokenStore : IAntiforgeryTokenStore
    {
        private readonly AntiforgeryOptions _options;

        public DefaultAntiforgeryTokenStore(IOptions<AntiforgeryOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;
        }

        public string GetCookieToken(HttpContext httpContext)
        {
            Debug.Assert(httpContext != null);

            var requestCookie = httpContext.Request.Cookies[_options.CookieName];
            if (string.IsNullOrEmpty(requestCookie))
            {
                // unable to find the cookie.
                return null;
            }

            return requestCookie;
        }

        public async Task<AntiforgeryTokenSet> GetRequestTokensAsync(HttpContext httpContext)
        {
            Debug.Assert(httpContext != null);

            var cookieToken = httpContext.Request.Cookies[_options.CookieName];

            StringValues requestToken;
            if (httpContext.Request.HasFormContentType)
            {
                // Check the content-type before accessing the form collection to make sure
                // we report errors gracefully.
                var form = await httpContext.Request.ReadFormAsync();
                requestToken = form[_options.FormFieldName];
            }

            // Fall back to header if the form value was not provided.
            if (requestToken.Count == 0 && _options.HeaderName != null)
            {
                requestToken = httpContext.Request.Headers[_options.HeaderName];
            }

            return new AntiforgeryTokenSet(requestToken, cookieToken, _options.FormFieldName, _options.HeaderName);
        }

        public void SaveCookieToken(HttpContext httpContext, string token)
        {
            Debug.Assert(httpContext != null);
            Debug.Assert(token != null);

            var options = new CookieOptions() { HttpOnly = true };

            // Note: don't use "newCookie.Secure = _options.RequireSSL;" since the default
            // value of newCookie.Secure is poulated out of band.
            if (_options.RequireSsl)
            {
                options.Secure = true;
            }

            httpContext.Response.Cookies.Append(_options.CookieName, token, options);
        }
    }
}