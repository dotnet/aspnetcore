// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The response type for the "/manage/passkeys" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class PasskeyRegistrationResponse
{
    /// <summary>
    /// The Base64Url-encoded credential ID for the registered passkey.
    /// </summary>
    public required string CredentialId { get; init; }

    /// <summary>
    /// The friendly name stored for the passkey.
    /// </summary>
    public string? Name { get; init; }
}
