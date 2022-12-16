// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents the result of trying to provision an access token.
/// </summary>
public class AccessTokenResult
{
    private readonly AccessToken _token;

    /// <summary>
    /// Initializes a new instance of <see cref="AccessTokenResult"/>.
    /// </summary>
    /// <param name="status">The status of the result.</param>
    /// <param name="token">The <see cref="AccessToken"/> in case it was successful.</param>
    /// <param name="redirectUrl">The redirect uri to go to for provisioning the token.</param>
    [Obsolete("Use the AccessTokenResult(AccessTokenResultStatus, AccessToken, string, InteractiveRequestOptions)")]
    public AccessTokenResult(AccessTokenResultStatus status, AccessToken token, [StringSyntax(StringSyntaxAttribute.Uri)] string redirectUrl)
    {
        Status = status;
        _token = token;
        RedirectUrl = redirectUrl;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="AccessTokenResult"/>.
    /// </summary>
    /// <param name="status">The status of the result.</param>
    /// <param name="token">The <see cref="AccessToken"/> in case it was successful.</param>
    /// <param name="interactiveRequestUrl">The redirect uri to go to for provisioning the token with <see cref="NavigationManagerExtensions.NavigateToLogin(NavigationManager, string, InteractiveRequestOptions)"/>.</param>
    /// <param name="interactiveRequest">The <see cref="InteractiveRequestOptions"/> containing the parameters for the interactive authentication.</param>
    public AccessTokenResult(AccessTokenResultStatus status, AccessToken token, [StringSyntax(StringSyntaxAttribute.Uri)] string? interactiveRequestUrl, InteractiveRequestOptions? interactiveRequest)
    {
        Status = status;
        _token = token;
        InteractiveRequestUrl = interactiveRequestUrl;
        InteractionOptions = interactiveRequest;
    }

    /// <summary>
    /// Gets the status of the current operation. See <see cref="AccessTokenResultStatus"/> for a list of statuses.
    /// </summary>
    public AccessTokenResultStatus Status { get; }

    /// <summary>
    /// Gets the URL to redirect to if <see cref="Status"/> is <see cref="AccessTokenResultStatus.RequiresRedirect"/>.
    /// </summary>
    [Obsolete("Use 'InteractiveRequestUrl' and 'InteractiveRequest' instead.")]
    public string? RedirectUrl { get; }

    /// <summary>
    /// Gets the URL to call <see cref="NavigationManagerExtensions.NavigateToLogin(NavigationManager, string, InteractiveRequestOptions)"/> if <see cref="Status"/> is
    /// <see cref="AccessTokenResultStatus.RequiresRedirect"/>.
    /// </summary>
    public string? InteractiveRequestUrl { get; }

    /// <summary>
    /// Gets the <see cref="InteractiveRequestOptions"/> to use if <see cref="Status"/> is <see cref="AccessTokenResultStatus.RequiresRedirect"/>.
    /// </summary>
    public InteractiveRequestOptions? InteractionOptions { get; }

    /// <summary>
    /// Determines whether the token request was successful and makes the <see cref="AccessToken"/> available for use when it is.
    /// </summary>
    /// <param name="accessToken">The <see cref="AccessToken"/> if the request was successful.</param>
    /// <returns><c>true</c> when the token request is successful; <c>false</c> otherwise.</returns>
    public bool TryGetToken([NotNullWhen(true)] out AccessToken? accessToken)
    {
        if (Status == AccessTokenResultStatus.Success)
        {
            accessToken = _token;
            return true;
        }
        else
        {
            accessToken = null;
            return false;
        }
    }
}
