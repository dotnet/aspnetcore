// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Mvc
{
    // Saves anti-XSRF tokens split between HttpRequest.Cookies and HttpRequest.Form
    internal sealed class AntiForgeryTokenStore : ITokenStore
    {
        private readonly AntiForgeryOptions _config;
        private readonly IAntiForgeryTokenSerializer _serializer;

        internal AntiForgeryTokenStore([NotNull] AntiForgeryOptions config, 
                                       [NotNull] IAntiForgeryTokenSerializer serializer)
        {
            _config = config;
            _serializer = serializer;
        }

        public AntiForgeryToken GetCookieToken(HttpContext httpContext)
        {
            var cookie = httpContext.Request.Cookies[_config.CookieName];
            if (string.IsNullOrEmpty(cookie))
            {
                // did not exist
                return null;
            }

            return _serializer.Deserialize(cookie);
        }

        public async Task<AntiForgeryToken> GetFormTokenAsync(HttpContext httpContext)
        {
            var form = await httpContext.Request.GetFormAsync();
            var value = form[_config.FormFieldName];
            if (string.IsNullOrEmpty(value))
            {
                // did not exist
                return null;
            }

            return _serializer.Deserialize(value);
        }

        public void SaveCookieToken(HttpContext httpContext, AntiForgeryToken token)
        {
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