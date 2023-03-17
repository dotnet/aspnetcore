// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Identity.Endpoints;

/// <summary>
/// Contains the options used to authenticate using bearer tokens issued by <see cref="IdentityEndpointRouteBuilderExtensions.MapIdentity{TUser}(IEndpointRouteBuilder)"/>.
/// </summary>
public sealed class IdentityBearerAuthenticationOptions : AuthenticationSchemeOptions
{
    /// <summary>
    /// Controls how much time the bearer token will remain valid from the point it is created.
    /// The expiration information is stored in the protected token. Because of that, an expired token will be rejected
    /// even if it is passed to the server after the client should have purged it.
    /// </summary>
    public TimeSpan BearerTokenExpiration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// If set, the <see cref="BearerTokenProtector"/> is used to protect and unprotect the identity and other properties which are stored in the
    /// bearer token value. If not provided, one will be created using <see cref="TicketDataFormat"/> and <see cref="DataProtectionProvider"/>.
    /// </summary>
    public ISecureDataFormat<AuthenticationTicket>? BearerTokenProtector { get; set; }

    /// <summary>
    /// If set, and <see cref="BearerTokenProtector"/> is not set, this will be used to protect the bearer token using <see cref="TicketDataFormat"/>.
    /// </summary>
    public IDataProtectionProvider? DataProtectionProvider { get; set; }

    /// <summary>
    /// If set, authentication and challenges will be forwarded to this scheme only if the request does not contain a bearer token.
    /// This is typically set to Usually Identity.Application cookies <see cref="IdentityConstants.ApplicationScheme"/>
    /// </summary>
    public string? BearerTokenMissingFallbackScheme { get; set; }
}
