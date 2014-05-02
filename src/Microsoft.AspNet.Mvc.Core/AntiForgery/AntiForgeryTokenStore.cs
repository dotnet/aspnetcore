// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using Microsoft.AspNet.Abstractions;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc
{
    // Saves anti-XSRF tokens split between HttpRequest.Cookies and HttpRequest.Form
    internal sealed class AntiForgeryTokenStore : ITokenStore
    {
        private readonly IAntiForgeryConfig _config;
        private readonly IAntiForgeryTokenSerializer _serializer;

        internal AntiForgeryTokenStore([NotNull] IAntiForgeryConfig config, 
                                       [NotNull] IAntiForgeryTokenSerializer serializer)
        {
            _config = config;
            _serializer = serializer;
        }

        public AntiForgeryToken GetCookieToken(HttpContext httpContext)
        {
            var cookie = httpContext.Request.Cookies[_config.CookieName];
            if (String.IsNullOrEmpty(cookie))
            {
                // did not exist
                return null;
            }

            return _serializer.Deserialize(cookie);
        }

        public async Task<AntiForgeryToken> GetFormTokenAsync(HttpContext httpContext)
        {
            var form = await httpContext.Request.GetFormAsync();
            string value = form[_config.FormFieldName];
            if (string.IsNullOrEmpty(value))
            {
                // did not exist
                return null;
            }

            return _serializer.Deserialize(value);
        }

        public void SaveCookieToken(HttpContext httpContext, AntiForgeryToken token)
        {
            string serializedToken = _serializer.Serialize(token);
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