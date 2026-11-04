// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Generates and validates antiforgery tokens.
/// </summary>
internal interface IAntiforgeryTokenGenerator
{
    /// <summary>
    /// Generates a new random cookie token.
    /// </summary>
    /// <returns>An <see cref="AntiforgeryToken"/>.</returns>
    AntiforgeryToken GenerateCookieToken();

    /// <summary>
    /// Generates a request token corresponding to <paramref name="cookieToken"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="cookieToken">A valid cookie token.</param>
    /// <returns>An <see cref="AntiforgeryToken"/>.</returns>
    AntiforgeryToken GenerateRequestToken(HttpContext httpContext, AntiforgeryToken cookieToken);

    /// <summary>
    /// Attempts to validate a cookie token.
    /// </summary>
    /// <param name="cookieToken">A valid cookie token.</param>
    /// <returns><c>true</c> if the cookie token is valid, otherwise <c>false</c>.</returns>
    bool IsCookieTokenValid(AntiforgeryToken? cookieToken);

    /// <summary>
    /// Attempts to validate a cookie and request token set for the given <paramref name="httpContext"/>.
    /// </summary>
    /// <param name="httpContext">The <see cref="HttpContext"/> associated with the current request.</param>
    /// <param name="cookieToken">A cookie token.</param>
    /// <param name="requestToken">A request token.</param>
    /// <param name="message">
    /// Will be set to the validation message if the tokens are invalid, otherwise <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the tokens are valid, otherwise <c>false</c>.</returns>
    bool TryValidateTokenSet(
        HttpContext httpContext,
        AntiforgeryToken cookieToken,
        AntiforgeryToken requestToken,
        [NotNullWhen(false)] out string? message);
}
