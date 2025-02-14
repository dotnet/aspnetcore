// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The request type for the "/refresh" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class RefreshRequest
{
    /// <summary>
    /// The <see cref="AccessTokenResponse.RefreshToken"/> from the last "/login" or "/refresh" response used to get a new <see cref="AccessTokenResponse"/>
    /// with an extended expiration.
    /// </summary>
    public required string RefreshToken { get; init; }
}
