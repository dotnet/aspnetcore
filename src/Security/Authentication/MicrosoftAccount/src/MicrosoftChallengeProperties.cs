// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.OAuth;

namespace Microsoft.AspNetCore.Authentication.MicrosoftAccount;

/// <summary>
/// <see cref="AuthenticationProperties"/> for Microsoft OAuth challenge request.
/// See <see href="https://learn.microsoft.com/azure/active-directory/develop/v2-oauth2-auth-code-flow#request-an-authorization-code"/> for reference.
/// </summary>
public class MicrosoftChallengeProperties : OAuthChallengeProperties
{
    /// <summary>
    /// The parameter key for the "response_mode" argument being used for a challenge request.
    /// </summary>
    [Obsolete("This parameter is not supported in MicrosoftAccountHandler.")]
    public static readonly string ResponseModeKey = "response_mode";

    /// <summary>
    /// The parameter key for the "domain_hint" argument being used for a challenge request.
    /// </summary>
    public static readonly string DomainHintKey = "domain_hint";

    /// <summary>
    /// The parameter key for the "login_hint" argument being used for a challenge request.
    /// </summary>
    public static readonly string LoginHintKey = "login_hint";

    /// <summary>
    /// The parameter key for the "prompt" argument being used for a challenge request.
    /// </summary>
    public static readonly string PromptKey = "prompt";

    /// <summary>
    /// Initializes a new instance for <see cref="MicrosoftChallengeProperties"/>.
    /// </summary>
    public MicrosoftChallengeProperties()
    { }

    /// <summary>
    /// Initializes a new instance for <see cref="MicrosoftChallengeProperties"/>.
    /// </summary>
    /// <inheritdoc />
    public MicrosoftChallengeProperties(IDictionary<string, string?> items)
        : base(items)
    { }

    /// <summary>
    /// Initializes a new instance for <see cref="MicrosoftChallengeProperties"/>.
    /// </summary>
    /// <inheritdoc />
    public MicrosoftChallengeProperties(IDictionary<string, string?> items, IDictionary<string, object?> parameters)
        : base(items, parameters)
    { }

    /// <summary>
    /// Gets or sets the value for the <c>response_mode</c> parameter used for a challenge request. The response mode specifies the method
    /// that should be used to send the resulting token back to the app. Can be one of the following: <c>query</c>, <c>fragment</c>, <c>form_post</c>.
    /// </summary>
    [Obsolete("This parameter is not supported in MicrosoftAccountHandler.")]
    public string? ResponseMode
    {
        get => GetParameter<string>(ResponseModeKey);
        set => SetParameter(ResponseModeKey, value);
    }

    /// <summary>
    /// Gets or sets the value for the "domain_hint" parameter value being used for a challenge request.
    /// <para>
    /// If included, authentication will skip the email-based discovery process that user goes through on the sign-in page,
    /// leading to a slightly more streamlined user experience.
    /// </para>
    /// </summary>
    public string? DomainHint
    {
        get => GetParameter<string>(DomainHintKey);
        set => SetParameter(DomainHintKey, value);
    }

    /// <summary>
    /// Gets or sets the value for the "login_hint" parameter value being used for a challenge request.
    /// <para>
    /// Can be used to pre-fill the username/email address field of the sign-in page for the user, if their username is known ahead of time.
    /// </para>
    /// </summary>
    public string? LoginHint
    {
        get => GetParameter<string>(LoginHintKey);
        set => SetParameter(LoginHintKey, value);
    }

    /// <summary>
    /// Gets or sets the value for the "prompt" parameter value being used for a challenge request.
    /// <para>
    /// Indicates the type of user interaction that is required. The only valid values at this time are login, none, and consent.
    /// </para>
    /// </summary>
    public string? Prompt
    {
        get => GetParameter<string>(PromptKey);
        set => SetParameter(PromptKey, value);
    }
}
