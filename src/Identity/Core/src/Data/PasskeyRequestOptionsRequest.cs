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
    /// Gets or initializes an email address reserved for a future opt-in mode that identifies the user.
    /// </summary>
    /// <remarks>
    /// This value is currently ignored. Request options only support discoverable credentials.
    /// </remarks>
    public string? Email { get; init; }
}
