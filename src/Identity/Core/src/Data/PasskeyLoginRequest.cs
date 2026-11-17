// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The request type for the "/passkeys/login" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class PasskeyLoginRequest
{
    /// <summary>
    /// The JSON-serialized credential returned by the browser's WebAuthn API.
    /// </summary>
    public required string CredentialJson { get; init; }
}
