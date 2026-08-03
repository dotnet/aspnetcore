// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the result of a passkey signal options generation.
/// </summary>
public sealed class PasskeySignalOptionsResult
{
    /// <summary>
    /// Gets or sets the JSON representation of the signal options.
    /// </summary>
    /// <remarks>
    /// The structure of this JSON is a superset of the options accepted by the
    /// <c>PublicKeyCredential.signalAllAcceptedCredentials()</c> and
    /// <c>PublicKeyCredential.signalCurrentUserDetails()</c> JavaScript APIs.
    /// See <see href="https://www.w3.org/TR/webauthn-3/#sctn-signal-methods"/>.
    /// </remarks>
    public required string SignalOptionsJson { get; init; }
}
