// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The request type for the "/passkeys/requestOptions" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class PasskeyRequestOptionsRequest
{
    /// <summary>
    /// The optional email address of the user requesting passkey options.
    /// </summary>
    public string? Email { get; init; }
}
