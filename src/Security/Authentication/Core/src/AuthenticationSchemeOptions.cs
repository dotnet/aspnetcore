// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Contains the options used by the <see cref="AuthenticationHandler{T}"/>.
/// </summary>
public class AuthenticationSchemeOptions
{
    /// <summary>
    /// Check that the options are valid. Should throw an exception if things are not ok.
    /// </summary>
    public virtual void Validate() { }

    /// <summary>
    /// Checks that the options are valid for a specific scheme
    /// </summary>
    /// <param name="scheme">The scheme being validated.</param>
    public virtual void Validate(string scheme)
        => Validate();

    /// <summary>
    /// Gets or sets the issuer that should be used for any claims that are created
    /// </summary>
    public string? ClaimsIssuer { get; set; }

    /// <summary>
    /// Instance used for events
    /// </summary>
    public object? Events { get; set; }

    /// <summary>
    /// If set, will be used as the service type to get the Events instance instead of the property.
    /// </summary>
    public Type? EventsType { get; set; }

    /// <summary>
    /// If set, this specifies a default scheme that authentication handlers should forward all authentication operations to
    /// by default. The default forwarding logic will check the most specific ForwardAuthenticate/Challenge/Forbid/SignIn/SignOut
    /// setting first, followed by checking the ForwardDefaultSelector, followed by ForwardDefault. The first non null result
    /// will be used as the target scheme to forward to.
    /// </summary>
    public string? ForwardDefault { get; set; }

    /// <summary>
    /// If set, this specifies the target scheme that this scheme should forward AuthenticateAsync calls to.
    /// For example Context.AuthenticateAsync("ThisScheme") => Context.AuthenticateAsync("ForwardAuthenticateValue");
    /// Set the target to the current scheme to disable forwarding and allow normal processing.
    /// </summary>
    public string? ForwardAuthenticate { get; set; }

    /// <summary>
    /// If set, this specifies the target scheme that this scheme should forward ChallengeAsync calls to.
    /// For example Context.ChallengeAsync("ThisScheme") => Context.ChallengeAsync("ForwardChallengeValue");
    /// Set the target to the current scheme to disable forwarding and allow normal processing.
    /// </summary>
    public string? ForwardChallenge { get; set; }

    /// <summary>
    /// If set, this specifies the target scheme that this scheme should forward ForbidAsync calls to.
    /// For example Context.ForbidAsync("ThisScheme") => Context.ForbidAsync("ForwardForbidValue");
    /// Set the target to the current scheme to disable forwarding and allow normal processing.
    /// </summary>
    public string? ForwardForbid { get; set; }

    /// <summary>
    /// If set, this specifies the target scheme that this scheme should forward SignInAsync calls to.
    /// For example Context.SignInAsync("ThisScheme") => Context.SignInAsync("ForwardSignInValue");
    /// Set the target to the current scheme to disable forwarding and allow normal processing.
    /// </summary>
    public string? ForwardSignIn { get; set; }

    /// <summary>
    /// If set, this specifies the target scheme that this scheme should forward SignOutAsync calls to.
    /// For example Context.SignOutAsync("ThisScheme") => Context.SignOutAsync("ForwardSignOutValue");
    /// Set the target to the current scheme to disable forwarding and allow normal processing.
    /// </summary>
    public string? ForwardSignOut { get; set; }

    /// <summary>
    /// Used to select a default scheme for the current request that authentication handlers should forward all authentication operations to
    /// by default. The default forwarding logic will check the most specific ForwardAuthenticate/Challenge/Forbid/SignIn/SignOut
    /// setting first, followed by checking the ForwardDefaultSelector, followed by ForwardDefault. The first non null result
    /// will be used as the target scheme to forward to.
    /// </summary>
    public Func<HttpContext, string?>? ForwardDefaultSelector { get; set; }
}
