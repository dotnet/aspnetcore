// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Antiforgery
{
    internal class DefaultAntiforgeryTokenStore : IAntiforgeryTokenStore
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

        public string? GetCookieToken(HttpContext httpContext)
        {
            Debug.Assert(httpContext != null);

            var requestCookie = httpContext.Request.Cookies[_options.Cookie.Name!];
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

            var cookieToken = httpContext.Request.Cookies[_options.Cookie.Name!];

            // We want to delay reading the form as much as possible, for example in case of large file uploads,
            // request token could be part of the header.
            StringValues requestToken = default;
            if (_options.HeaderName != null)
            {
                requestToken = httpContext.Request.Headers[_options.HeaderName];
            }

            // Fall back to reading form instead
            if (requestToken.Count == 0 && httpContext.Request.HasFormContentType)
            {
                // Check the content-type before accessing the form collection to make sure
                // we report errors gracefully.
                try
                {
                    var form = await httpContext.Request.ReadFormAsync();
                    requestToken = form[_options.FormFieldName];
                }
                catch (IOException ex)
                {
                    // Reading the request body (which happens as part of ReadFromAsync) may throw an exception if a client disconnects.
                    // Rethrow this as an AntiforgeryException and allow the caller to handle it as just another antiforgery failure.
                    throw new AntiforgeryValidationException(Resources.AntiforgeryToken_UnableToReadRequest, ex);
                }
            }

            return new AntiforgeryTokenSet(requestToken, cookieToken, _options.FormFieldName, _options.HeaderName);
        }

        public void SaveCookieToken(HttpContext httpContext, string token)
        {
            Debug.Assert(httpContext != null);
            Debug.Assert(token != null);

            var options = _options.Cookie.Build(httpContext);

            if (_options.Cookie.Path != null)
            {
                options.Path = _options.Cookie.Path;
            }
            else
            {
                var pathBase = httpContext.Request.PathBase.ToString();
                if (!string.IsNullOrEmpty(pathBase))
                {
                    options.Path = pathBase;
                }
            }

            httpContext.Response.Cookies.Append(_options.Cookie.Name!, token, options);
        }
    }
}
