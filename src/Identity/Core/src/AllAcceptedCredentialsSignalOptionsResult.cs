// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the result of an all accepted credentials signal options generation.
/// </summary>
public sealed class AllAcceptedCredentialsSignalOptionsResult
{
    /// <summary>
    /// Gets or sets the JSON representation of the all accepted credentials signal options.
    /// </summary>
    /// <remarks>
    /// The structure of this JSON is compatible with
    /// <see href="https://www.w3.org/TR/webauthn-3/#dictdef-allacceptedcredentialsoptions"/>
    /// and should be passed unchanged to the <c>PublicKeyCredential.signalAllAcceptedCredentials()</c>
    /// JavaScript API.
    /// </remarks>
    /// <example>
    /// The following example shows how the JSON is used from JavaScript.
    /// <code language="javascript">
    /// await PublicKeyCredential.signalAllAcceptedCredentials?.(JSON.parse(signalOptionsJson));
    /// </code>
    /// </example>
    public required string SignalOptionsJson { get; init; }
}
