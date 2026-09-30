// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the result of an unknown credential signal options generation.
/// </summary>
public sealed class UnknownCredentialSignalOptionsResult
{
    /// <summary>
    /// Gets or sets the JSON representation of the unknown credential signal options.
    /// </summary>
    /// <remarks>
    /// The structure of this JSON is compatible with
    /// <see href="https://www.w3.org/TR/webauthn-3/#dictdef-unknowncredentialoptions"/>
    /// and should be passed unchanged to the <c>PublicKeyCredential.signalUnknownCredential()</c>
    /// JavaScript API. Calling that API permanently deletes the passkey from the browser's passkey provider.
    /// </remarks>
    /// <example>
    /// The following example shows how the JSON is used from JavaScript.
    /// <code language="javascript">
    /// await PublicKeyCredential.signalUnknownCredential?.(JSON.parse(signalOptionsJson));
    /// </code>
    /// </example>
    public required string SignalOptionsJson { get; init; }
}
