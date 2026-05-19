// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The response type for the "/forgotPassword" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class ForgotPasswordRequest
{
    /// <summary>
    /// The email address to send the reset password code to if a user with that confirmed email address already exists.
    /// </summary>
    public required string Email { get; init; }
}
