// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Antiforgery;

/// <summary>
/// Allows providing or validating additional custom data for antiforgery tokens.
/// For example, the developer could use this to supply a nonce when the token is
/// generated, then validate it when the token is validated.
/// </summary>
/// <remarks>
/// The antiforgery system already embeds the client's username within the
/// generated tokens. This interface provides and consumes <em>supplemental</em>
/// data. If an incoming antiforgery token contains supplemental data but no
/// additional data provider is configured, the supplemental data will not be
/// validated.
/// </remarks>
public interface IAntiforgeryAdditionalDataProvider
{
    /// <summary>
    /// Provides additional data to be stored for the antiforgery tokens generated
    /// during this request.
    /// </summary>
    /// <param name="context">Information about the current request.</param>
    /// <returns>Supplemental data to embed within the antiforgery token.</returns>
    string GetAdditionalData(HttpContext context);

    /// <summary>
    /// Validates additional data that was embedded inside an incoming antiforgery
    /// token.
    /// </summary>
    /// <param name="context">Information about the current request.</param>
    /// <param name="additionalData">Supplemental data that was embedded within the token.</param>
    /// <returns>True if the data is valid; false if the data is invalid.</returns>
    bool ValidateAdditionalData(HttpContext context, string additionalData);
}
