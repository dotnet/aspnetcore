// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Base context for events that produce AuthenticateResults.
/// </summary>
public abstract class ResultContext<TOptions> : BaseContext<TOptions> where TOptions : AuthenticationSchemeOptions
{
    private AuthenticationProperties? _properties;

    /// <summary>
    /// Initializes a new instance of <see cref="ResultContext{TOptions}"/>.
    /// </summary>
    /// <param name="context">The context.</param>
    /// <param name="scheme">The authentication scheme.</param>
    /// <param name="options">The authentication options associated with the scheme.</param>
    protected ResultContext(HttpContext context, AuthenticationScheme scheme, TOptions options)
        : base(context, scheme, options) { }

    /// <summary>
    /// Gets or sets the <see cref="ClaimsPrincipal"/> containing the user claims.
    /// </summary>
    public ClaimsPrincipal? Principal { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="AuthenticationProperties"/>.
    /// </summary>
    public AuthenticationProperties Properties
    {
        get
        {
            _properties ??= new AuthenticationProperties();
            return _properties;
        }
        set => _properties = value;
    }

    /// <summary>
    /// Gets the <see cref="AuthenticateResult"/> result.
    /// </summary>
    public AuthenticateResult? Result { get; private set; }

    /// <summary>
    /// Calls success creating a ticket with the <see cref="Principal"/> and <see cref="Properties"/>.
    /// </summary>
    public void Success() => Result = HandleRequestResult.Success(new AuthenticationTicket(Principal!, Properties, Scheme.Name));

    /// <summary>
    /// Indicates that there was no information returned for this authentication scheme.
    /// </summary>
    public void NoResult() => Result = AuthenticateResult.NoResult();

    /// <summary>
    /// Indicates that there was a failure during authentication.
    /// </summary>
    /// <param name="failure"></param>
    public void Fail(Exception failure) => Result = AuthenticateResult.Fail(failure);

    /// <summary>
    /// Indicates that there was a failure during authentication.
    /// </summary>
    /// <param name="failureMessage"></param>
    public void Fail(string failureMessage) => Result = AuthenticateResult.Fail(failureMessage);
}
