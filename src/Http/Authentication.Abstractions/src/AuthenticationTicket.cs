// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Contains user identity information as well as additional authentication state.
/// </summary>
public class AuthenticationTicket
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationTicket"/> class
    /// </summary>
    /// <param name="principal">the <see cref="ClaimsPrincipal"/> that represents the authenticated user.</param>
    /// <param name="properties">additional properties that can be consumed by the user or runtime.</param>
    /// <param name="authenticationScheme">the authentication scheme that was responsible for this ticket.</param>
    public AuthenticationTicket(ClaimsPrincipal principal, AuthenticationProperties? properties, string authenticationScheme)
    {
        ArgumentNullException.ThrowIfNull(principal);

        AuthenticationScheme = authenticationScheme;
        Principal = principal;
        Properties = properties ?? new AuthenticationProperties();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationTicket"/> class
    /// </summary>
    /// <param name="principal">the <see cref="ClaimsPrincipal"/> that represents the authenticated user.</param>
    /// <param name="authenticationScheme">the authentication scheme that was responsible for this ticket.</param>
    public AuthenticationTicket(ClaimsPrincipal principal, string authenticationScheme)
        : this(principal, properties: null, authenticationScheme: authenticationScheme)
    { }

    /// <summary>
    /// Gets the authentication scheme that was responsible for this ticket.
    /// </summary>
    public string AuthenticationScheme { get; }

    /// <summary>
    /// Gets the claims-principal with authenticated user identities.
    /// </summary>
    public ClaimsPrincipal Principal { get; }

    /// <summary>
    /// Additional state values for the authentication session.
    /// </summary>
    public AuthenticationProperties Properties { get; }

    /// <summary>
    /// Returns a copy of the ticket.
    /// </summary>
    /// <remarks>
    /// The method clones the <see cref="Principal"/> by calling <see cref="ClaimsIdentity.Clone"/> on each of the <see cref="ClaimsPrincipal.Identities"/>.
    /// </remarks>
    /// <returns>A copy of the ticket</returns>
    public AuthenticationTicket Clone()
    {
        var principal = new ClaimsPrincipal();
        foreach (var identity in Principal.Identities)
        {
            principal.AddIdentity(identity.Clone());
        }
        return new AuthenticationTicket(principal, Properties.Clone(), AuthenticationScheme);
    }
}
