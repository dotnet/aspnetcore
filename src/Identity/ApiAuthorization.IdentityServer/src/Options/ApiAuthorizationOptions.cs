// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
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
        /// Gets or sets the <see cref="Clients"/>.
        /// </summary>
        public ClientCollection Clients { get; set; } =
            new ClientCollection();

        /// <summary>
        /// Gets or sets the <see cref="SigningCredentials"/> to use for signing tokens.
        /// </summary>
        public SigningCredentials SigningCredential { get; set; }
    }
}
