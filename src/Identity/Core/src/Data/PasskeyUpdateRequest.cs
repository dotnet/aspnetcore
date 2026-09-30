// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The request type for the "/manage/passkeys/{credentialId}" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class PasskeyUpdateRequest
{
    /// <summary>
    /// The friendly name to store for the passkey. If empty, any existing name is cleared.
    /// </summary>
    public string? Name { get; init; }
}
