// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer;

/// <summary>
/// Options for API authorization.
/// </summary>
public class ApiAuthorizationOptions
{
    /// <summary>
    /// Gets or sets the <see cref="IdentityResources"/>.
    /// </summary>
    public IdentityResourceCollection IdentityResources { get; set; } =
        new IdentityResourceCollection
        {
                IdentityResourceBuilder.OpenId()
                    .AllowAllClients()
                    .FromDefault()
                    .Build(),
                IdentityResourceBuilder.Profile()
                    .AllowAllClients()
                    .FromDefault()
                    .Build()
        };

    /// <summary>
    /// Gets or sets the <see cref="ApiResources"/>.
    /// </summary>
    public ApiResourceCollection ApiResources { get; set; } =
        new ApiResourceCollection();

    /// <summary>
    /// Gets or sets the <see cref="ApiScopes"/>.
    /// </summary>
    public ApiScopeCollection ApiScopes { get; set; } =
        new ApiScopeCollection();

    /// <summary>
    /// Gets or sets the <see cref="Clients"/>.
    /// </summary>
    public ClientCollection Clients { get; set; } =
        new ClientCollection();

    /// <summary>
    /// Gets or sets the <see cref="SigningCredentials"/> to use for signing tokens.
    /// </summary>
    public SigningCredentials SigningCredential { get; set; }
}
