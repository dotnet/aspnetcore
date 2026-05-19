// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the result of a passkey request options generation.
/// </summary>
public sealed class PasskeyRequestOptionsResult
{
    /// <summary>
    /// Gets or sets the JSON representation of the request options.
    /// </summary>
    /// <remarks>
    /// The structure of this JSON is compatible with
    /// <see href="https://www.w3.org/TR/webauthn-3/#dictdef-publickeycredentialrequestoptionsjson"/>
    /// and should be used with the <c>navigator.credentials.get()</c> JavaScript API.
    /// </remarks>
    public required string RequestOptionsJson { get; init; }

    /// <summary>
    /// Gets or sets the state to be used in the assertion procedure.
    /// </summary>
    /// <remarks>
    /// This can be later retrieved during assertion with <see cref="PasskeyAssertionContext.AssertionState"/>.
    /// </remarks>
    public string? AssertionState { get; init; }
}
