// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

/// <summary>
/// Represents options to pass down to configure the oidc-client.js library used when using a standard OpenID Connect (OIDC) flow.
/// </summary>
public class OidcProviderOptions
{
    /// <summary>
    /// Gets or sets the authority of the OpenID Connect (OIDC) identity provider.
    /// </summary>
    public string? Authority { get; set; }

    /// <summary>
    /// Gets or sets the metadata URL of the OpenID Connect (OIDC) provider.
    /// </summary>
    public string? MetadataUrl { get; set; }

    /// <summary>
    /// Gets or sets the client of the application.
    /// </summary>
    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    /// <summary>
    /// Gets or sets the list of scopes to request when signing in.
    /// </summary>
    /// <value>Defaults to <c>openid</c> and <c>profile</c>.</value>
    public IList<string> DefaultScopes { get; } = new List<string> { "openid", "profile" };

    /// <summary>
    /// Gets or sets the redirect URI for the application. The application will be redirected here after the user has completed the sign in
    /// process from the identity provider.
    /// </summary>
    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; set; }

    /// <summary>
    /// Gets or sets the post logout redirect URI for the application. The application will be redirected here after the user has completed the sign out
    /// process from the identity provider.
    /// </summary>
    [JsonPropertyName("post_logout_redirect_uri")]
    public string? PostLogoutRedirectUri { get; set; }

    /// <summary>
    /// Gets or sets the response type to use on the authorization flow. The valid values are specified by the identity provider metadata.
    /// </summary>
    [JsonPropertyName("response_type")]
    public string? ResponseType { get; set; }

    /// <summary>
    /// Gets or sets the response mode to use in the authorization flow.
    /// </summary>
    [JsonPropertyName("response_mode")]
    public string? ResponseMode { get; set; }

    /// <summary>
    /// Gets or sets the additional provider parameters to use on the authorization flow.
    /// </summary>
    /// <remarks>
    /// These parameters are for the IdP and not for the application. Using those parameters in the application in any way on the login callback will likely introduce security issues as they should be treated as untrusted input.
    /// </remarks>
    [JsonPropertyName("extraQueryParams")]
    public IDictionary<string, string> AdditionalProviderParameters { get; } = new Dictionary<string, string>();
}
