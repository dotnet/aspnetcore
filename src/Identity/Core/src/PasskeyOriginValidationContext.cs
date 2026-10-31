// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Contains information used for determining whether a passkey's origin is valid.
/// </summary>
public sealed class PasskeyOriginValidationContext
{
    /// <summary>
    /// Gets or sets the HTTP context associated with the request.
    /// </summary>
    public required HttpContext HttpContext { get; init; }

    /// <summary>
    /// Gets or sets the fully-qualified origin of the requester.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-collectedclientdata-origin"/>.
    /// </remarks>
    public required string Origin { get; init; }

    /// <summary>
    /// Gets or sets whether the request came from a cross-origin <c>&lt;iframe&gt;</c>.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-collectedclientdata-crossorigin"/>.
    /// </remarks>
    public required bool CrossOrigin { get; init; }

    /// <summary>
    /// Gets or sets the fully-qualified top-level origin of the requester.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#dom-collectedclientdata-toporigin"/>.
    /// </remarks>
    public string? TopOrigin { get; init; }
}
