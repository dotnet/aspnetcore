// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using IdentityServer4;
using IdentityServer4.Models;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    /// <summary>
    /// A builder for identity resources
    /// </summary>
    public class IdentityResourceBuilder
    {
        private IdentityResource _identityResource;
        private bool _built;

        /// <summary>
        /// Creates an openid resource.
        /// </summary>
        public static IdentityResourceBuilder OpenId() =>
            IdentityResource(IdentityServerConstants.StandardScopes.OpenId);

        /// <summary>
        /// Creates a profile resource.
        /// </summary>
        public static IdentityResourceBuilder Profile() =>
            IdentityResource(IdentityServerConstants.StandardScopes.Profile);

        /// <summary>
        /// Creates an address resource.
        /// </summary>
        public static IdentityResourceBuilder Address() =>
            IdentityResource(IdentityServerConstants.StandardScopes.Address);

        /// <summary>
        /// Creates an email resource.
        /// </summary>
        public static IdentityResourceBuilder Email() =>
            IdentityResource(IdentityServerConstants.StandardScopes.Email);

        /// <summary>
        /// Creates a phone resource.
        /// </summary>
        public static IdentityResourceBuilder Phone() =>
            IdentityResource(IdentityServerConstants.StandardScopes.Phone);

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityResourceBuilder"/>.
        /// </summary>
        public IdentityResourceBuilder() : this(new IdentityResource())
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityResourceBuilder"/>.
        /// </summary>
        /// <param name="resource">A preconfigured resource.</param>
        public IdentityResourceBuilder(IdentityResource resource)
        {
            _identityResource = resource;
        }

        /// <summary>
        /// Configures the API resource to allow all clients to access it.
        /// </summary>
        /// <returns>The <see cref="IdentityResourceBuilder"/>.</returns>
        public IdentityResourceBuilder AllowAllClients()
        {
            _identityResource.Properties[ApplicationProfilesPropertyNames.Clients] = ApplicationProfilesPropertyValues.AllowAllApplications;
            return this;
        }

        /// <summary>
        /// Builds the API resource.
        /// </summary>
        /// <returns>The built <see cref="IdentityServer4.Models.IdentityResource"/>.</returns>
        public IdentityResource Build()
        {
            if (_built)
            {
                throw new InvalidOperationException("IdentityResource already built.");
            }

            _built = true;
            return _identityResource;
        }

        internal IdentityResourceBuilder WithAllowedClients(string clientList)
        {
            _identityResource.Properties[ApplicationProfilesPropertyNames.Clients] = clientList;
            return this;
        }

        internal IdentityResourceBuilder FromConfiguration()
        {
            _identityResource.Properties[ApplicationProfilesPropertyNames.Source] = ApplicationProfilesPropertyValues.Configuration;
            return this;
        }

        internal IdentityResourceBuilder FromDefault()
        {
            _identityResource.Properties[ApplicationProfilesPropertyNames.Source] = ApplicationProfilesPropertyValues.Default;
            return this;
        }

        internal static IdentityResourceBuilder IdentityResource(string name)
        {
            var identityResource = GetResource(name);
            return new IdentityResourceBuilder(identityResource);
        }

        private static IdentityResource GetResource(string name)
        {
            switch (name)
            {
                case IdentityServerConstants.StandardScopes.OpenId:
                    return new IdentityResources.OpenId();
                case IdentityServerConstants.StandardScopes.Profile:
                    return new IdentityResources.Profile();
                case IdentityServerConstants.StandardScopes.Address:
                    return new IdentityResources.Address();
                case IdentityServerConstants.StandardScopes.Email:
                    return new IdentityResources.Email();
                case IdentityServerConstants.StandardScopes.Phone:
                    return new IdentityResources.Phone();
                default:
                    throw new InvalidOperationException("Invalid identity resource type.");
            }
        }
    }
}