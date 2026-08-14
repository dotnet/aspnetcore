// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the result of an unknown passkey signal options generation.
/// </summary>
public sealed class UnknownPasskeySignalOptionsResult
{
    /// <summary>
    /// Gets or sets the JSON representation of the unknown passkey signal options.
    /// </summary>
    /// <remarks>
    /// The JSON is accepted by the <c>PublicKeyCredential.signalUnknownCredential()</c> JavaScript API.
    /// Calling this API permanently deletes the passkey from the browser's passkey provider.
    /// </remarks>
    /// <example>
    /// <code language="javascript">
    /// const signalOptions = JSON.parse(signalOptionsJson);
    /// await PublicKeyCredential.signalUnknownCredential?.(signalOptions);
    /// </code>
    /// </example>
    public required string SignalOptionsJson { get; init; }
}
