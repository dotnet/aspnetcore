// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Antiforgery
{
    // Saves anti-XSRF tokens split between HttpRequest.Cookies and HttpRequest.Form
    public class DefaultAntiforgeryTokenStore : IAntiforgeryTokenStore
    {
        private readonly AntiforgeryOptions _options;
        private readonly IAntiforgeryTokenSerializer _tokenSerializer;

        public DefaultAntiforgeryTokenStore(
            [NotNull] IOptions<AntiforgeryOptions> optionsAccessor,
            [NotNull] IAntiforgeryTokenSerializer tokenSerializer)
        {
            _options = optionsAccessor.Options;
            _tokenSerializer = tokenSerializer;
        }

        public AntiforgeryToken GetCookieToken(HttpContext httpContext)
        {
            var services = httpContext.RequestServices;
            var contextAccessor = services.GetRequiredService<IAntiforgeryContextAccessor>();
            if (contextAccessor.Value != null)
            {
                return contextAccessor.Value.CookieToken;
            }

            var requestCookie = httpContext.Request.Cookies[_options.CookieName];
            if (string.IsNullOrEmpty(requestCookie))
            {
                // unable to find the cookie.
                return null;
            }

            return _tokenSerializer.Deserialize(requestCookie);
        }

        public async Task<AntiforgeryToken> GetFormTokenAsync(HttpContext httpContext)
        {
            var form = await httpContext.Request.ReadFormAsync();
            var value = form[_options.FormFieldName];
            if (string.IsNullOrEmpty(value))
            {
                // did not exist
                return null;
            }

            return _tokenSerializer.Deserialize(value);
        }

        public void SaveCookieToken(HttpContext httpContext, AntiforgeryToken token)
        {
            // Add the cookie to the request based context.
            // This is useful if the cookie needs to be reloaded in the context of the same request.

            var services = httpContext.RequestServices;
            var contextAccessor = services.GetRequiredService<IAntiforgeryContextAccessor>();
            Debug.Assert(contextAccessor.Value == null, "AntiforgeryContext should be set only once per request.");
            contextAccessor.Value = new AntiforgeryContext() { CookieToken = token };

            var serializedToken = _tokenSerializer.Serialize(token);
            var options = new CookieOptions() { HttpOnly = true };

            // Note: don't use "newCookie.Secure = _options.RequireSSL;" since the default
            // value of newCookie.Secure is poulated out of band.
            if (_options.RequireSsl)
            {
                options.Secure = true;
            }

            httpContext.Response.Cookies.Append(_options.CookieName, serializedToken, options);
        }
    }
}