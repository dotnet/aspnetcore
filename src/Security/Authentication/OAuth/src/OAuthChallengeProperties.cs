// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.OAuth;

/// <summary>
/// <see cref="AuthenticationProperties"/> for an OAuth challenge.
/// </summary>
public class OAuthChallengeProperties : AuthenticationProperties
{
    /// <summary>
    /// The parameter key for the "scope" argument being used for a challenge request.
    /// </summary>
    public static readonly string ScopeKey = "scope";

    /// <summary>
    /// Initializes a new instance of <see cref="OAuthChallengeProperties"/>.
    /// </summary>
    public OAuthChallengeProperties()
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="OAuthChallengeProperties" />.
    /// </summary>
    /// <inheritdoc />
    public OAuthChallengeProperties(IDictionary<string, string?> items)
        : base(items)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="OAuthChallengeProperties" />.
    /// </summary>
    /// <inheritdoc />
    public OAuthChallengeProperties(IDictionary<string, string?>? items, IDictionary<string, object?>? parameters)
        : base(items, parameters)
    { }

    /// <summary>
    /// The "scope" parameter value being used for a challenge request.
    /// </summary>
    public ICollection<string> Scope
    {
        get => GetParameter<ICollection<string>>(ScopeKey)!;
        set => SetParameter(ScopeKey, value);
    }

    /// <summary>
    /// Set the "scope" parameter value.
    /// </summary>
    /// <param name="scopes">List of scopes.</param>
    public virtual void SetScope(params string[] scopes)
    {
        Scope = scopes;
    }
}
