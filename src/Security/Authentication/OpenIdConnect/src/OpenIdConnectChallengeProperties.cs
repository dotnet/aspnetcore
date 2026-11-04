// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect;

/// <summary>
/// <see cref="AuthenticationProperties"/> for an OpenId Connect challenge.
/// </summary>
public class OpenIdConnectChallengeProperties : OAuthChallengeProperties
{
    /// <summary>
    /// The parameter key for the "max_age" argument being used for a challenge request.
    /// </summary>
    public static readonly string MaxAgeKey = OpenIdConnectParameterNames.MaxAge;

    /// <summary>
    /// The parameter key for the "prompt" argument being used for a challenge request.
    /// </summary>
    public static readonly string PromptKey = OpenIdConnectParameterNames.Prompt;

    /// <summary>
    /// Initializes a new instance of <see cref="OpenIdConnectChallengeProperties"/>.
    /// </summary>
    public OpenIdConnectChallengeProperties()
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="OpenIdConnectChallengeProperties"/>.
    /// </summary>
    /// <inheritdoc />
    public OpenIdConnectChallengeProperties(IDictionary<string, string?> items)
        : base(items)
    { }

    /// <summary>
    /// Initializes a new instance of <see cref="OpenIdConnectChallengeProperties"/>.
    /// </summary>
    /// <inheritdoc />
    public OpenIdConnectChallengeProperties(IDictionary<string, string?> items, IDictionary<string, object?> parameters)
        : base(items, parameters)
    { }

    /// <summary>
    /// The "max_age" parameter value being used for a challenge request.
    /// </summary>
    public TimeSpan? MaxAge
    {
        get => GetParameter<TimeSpan?>(MaxAgeKey);
        set => SetParameter(MaxAgeKey, value);
    }

    /// <summary>
    /// The "prompt" parameter value being used for a challenge request.
    /// </summary>
    public string? Prompt
    {
        get => GetParameter<string>(PromptKey);
        set => SetParameter(PromptKey, value);
    }
}
