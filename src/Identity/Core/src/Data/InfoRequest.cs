// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The request type for the "/manage/info" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// All properties are optional. No modifications will be made to the user if all the properties are omitted from the request.
/// </summary>
public sealed class InfoRequest
{
    /// <summary>
    /// The optional new email address for the authenticated user. This will replace the old email address if there was one. The email will not be updated until it is confirmed.
    /// </summary>
    public string? NewEmail { get; init; }

    /// <summary>
    /// The optional new password for the authenticated user. If a new password is provided, the <see cref="OldPassword"/> is required.
    /// If the user forgot the old password, use the "/forgotPassword" endpoint instead.
    /// </summary>
    public string? NewPassword { get; init; }

    /// <summary>
    /// The old password for the authenticated user. This is only required if a <see cref="NewPassword"/> is provided.
    /// </summary>
    public string? OldPassword { get; init; }
}
