// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Data;

/// <summary>
/// The response type for the "/resendConfirmationEmail" endpoint added by <see cref="IdentityApiEndpointRouteBuilderExtensions.MapIdentityApi"/>.
/// </summary>
public sealed class ResendConfirmationEmailRequest
{
    /// <summary>
    /// The email address to resend the confirmation email to if a user with that email exists.
    /// </summary>
    public required string Email { get; init; }
}
