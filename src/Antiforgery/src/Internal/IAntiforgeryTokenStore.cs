// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

internal interface IAntiforgeryTokenStore
{
    string? GetCookieToken(HttpContext httpContext);

    /// <summary>
    /// Gets the cookie and request tokens from the request.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> for the current request.</param>
    /// <returns>The <see cref="AntiforgeryTokenSet"/>.</returns>
    Task<AntiforgeryTokenSet> GetRequestTokensAsync(HttpContext httpContext);

    void SaveCookieToken(HttpContext httpContext, string token);
}
