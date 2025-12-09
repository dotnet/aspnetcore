// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class DefaultAntiforgeryTokenStore : IAntiforgeryTokenStore
{
    private readonly AntiforgeryOptions _options;

    public DefaultAntiforgeryTokenStore(IOptions<AntiforgeryOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);

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
        if (requestToken.Count == 0 && httpContext.Request.HasFormContentType && !_options.SuppressReadingTokenFromFormBody)
        {
            // Check the content-type before accessing the form collection to make sure
            // we report errors gracefully.
            IFormCollection form;
            try
            {
                form = await httpContext.Request.ReadFormAsync();
            }
            catch (InvalidDataException ex)
            {
                // ReadFormAsync can throw InvalidDataException if the form content is malformed.
                // Wrap it in an AntiforgeryValidationException and allow the caller to handle it as just another antiforgery failure.
                throw new AntiforgeryValidationException(Resources.AntiforgeryToken_UnableToReadRequest, ex);
            }
            catch (IOException ex)
            {
                // Reading the request body (which happens as part of ReadFromAsync) may throw an exception if a client disconnects.
                // Wrap it in an AntiforgeryValidationException and allow the caller to handle it as just another antiforgery failure.
                throw new AntiforgeryValidationException(Resources.AntiforgeryToken_UnableToReadRequest, ex);
            }

            requestToken = form[_options.FormFieldName];
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
