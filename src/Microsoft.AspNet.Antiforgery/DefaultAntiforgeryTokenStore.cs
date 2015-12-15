// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.Antiforgery
{
    public class DefaultAntiforgeryTokenStore : IAntiforgeryTokenStore
    {
        private readonly AntiforgeryOptions _options;
        private readonly IAntiforgeryTokenSerializer _tokenSerializer;

        public DefaultAntiforgeryTokenStore(
            IOptions<AntiforgeryOptions> optionsAccessor,
            IAntiforgeryTokenSerializer tokenSerializer)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            if (tokenSerializer == null)
            {
                throw new ArgumentNullException(nameof(tokenSerializer));
            }

            _options = optionsAccessor.Value;
            _tokenSerializer = tokenSerializer;
        }

        public AntiforgeryToken GetCookieToken(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

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

        public async Task<AntiforgeryTokenSet> GetRequestTokensAsync(HttpContext httpContext)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            var requestCookie = httpContext.Request.Cookies[_options.CookieName];
            if (string.IsNullOrEmpty(requestCookie))
            {
                throw new InvalidOperationException(
                    Resources.FormatAntiforgery_CookieToken_MustBeProvided(_options.CookieName));
            }

            StringValues requestToken;
            if (httpContext.Request.HasFormContentType)
            {
                // Check the content-type before accessing the form collection to make sure
                // we throw gracefully.
                var form = await httpContext.Request.ReadFormAsync();
                requestToken = form[_options.FormFieldName];
            }

            // Fall back to header if the form value was not provided.
            if (requestToken.Count == 0 && _options.HeaderName != null)
            {
                requestToken = httpContext.Request.Headers[_options.HeaderName];
            }

            if (requestToken.Count == 0)
            {
                if (_options.HeaderName == null)
                {
                    var message = Resources.FormatAntiforgery_FormToken_MustBeProvided(_options.FormFieldName);
                    throw new InvalidOperationException(message);
                }
                else if (!httpContext.Request.HasFormContentType)
                {
                    var message = Resources.FormatAntiforgery_HeaderToken_MustBeProvided(_options.HeaderName);
                    throw new InvalidOperationException(message);
                }
                else
                {
                    var message = Resources.FormatAntiforgery_RequestToken_MustBeProvided(
                        _options.FormFieldName,
                        _options.HeaderName);
                    throw new InvalidOperationException(message);
                }
            }

            return new AntiforgeryTokenSet(requestToken, requestCookie);
        }

        public void SaveCookieToken(HttpContext httpContext, AntiforgeryToken token)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

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