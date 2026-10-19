// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents the result of a current user details signal options generation.
/// </summary>
public sealed class CurrentUserDetailsSignalOptionsResult
{
    /// <summary>
    /// Gets or sets the JSON representation of the current user details signal options.
    /// </summary>
    /// <remarks>
    /// The structure of this JSON is compatible with
    /// <see href="https://www.w3.org/TR/webauthn-3/#dictdef-currentuserdetailsoptions"/>
    /// and should be passed unchanged to the <c>PublicKeyCredential.signalCurrentUserDetails()</c>
    /// JavaScript API.
    /// </remarks>
    /// <example>
    /// The following example shows how the JSON is used from JavaScript.
    /// <code language="javascript">
    /// await PublicKeyCredential.signalCurrentUserDetails?.(JSON.parse(signalOptionsJson));
    /// </code>
    /// </example>
    public required string SignalOptionsJson { get; init; }
}
