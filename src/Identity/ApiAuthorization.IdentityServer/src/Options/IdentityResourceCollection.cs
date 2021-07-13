// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Duende.IdentityServer.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    /// <summary>
    /// A collection of <see cref="IdentityResource"/>.
    /// </summary>
    public class IdentityResourceCollection : Collection<IdentityResource>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="IdentityResourceCollection"/>.
        /// </summary>
        public IdentityResourceCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="IdentityResourceCollection"/> with the given
        /// identity resources in <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The initial list of <see cref="IdentityResource"/>.</param>
        public IdentityResourceCollection(IList<IdentityResource> list) : base(list)
        {
        }

        /// <summary>
        /// Gets an identity resource given its name.
        /// </summary>
        /// <param name="key">The name of the <see cref="IdentityResource"/>.</param>
        /// <returns>The <see cref="IdentityResource"/>.</returns>
        public IdentityResource this[string key]
        {
            get
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    var candidate = Items[i];
                    if (string.Equals(candidate.Name, key, StringComparison.Ordinal))
                    {
                        return candidate;
                    }
                }

                throw new InvalidOperationException($"IdentityResource '{key}' not found.");
            }
        }

        /// <summary>
        /// Adds the identity resources in <paramref name="identityResources"/> to the collection.
        /// </summary>
        /// <param name="identityResources">The list of <see cref="IdentityResource"/> to add.</param>
        public void AddRange(params IdentityResource[] identityResources)
        {
            foreach (var resource in identityResources)
            {
                Add(resource);
            }
        }

        /// <summary>
        /// Adds an openid resource.
        /// </summary>
        public void AddOpenId() =>
            Add(IdentityResourceBuilder.OpenId().Build());

        /// <summary>
        /// Adds an openid resource.
        /// </summary>
        /// <param name="configure">The <see cref="Action{IdentityResourceBuilder}"/> to configure the openid scope.</param>
        public void AddOpenId(Action<IdentityResourceBuilder> configure)
        {
            var resource = IdentityResourceBuilder.OpenId();
            configure(resource);
            Add(resource.Build());
        }

        /// <summary>
        /// Adds a profile resource.
        /// </summary>
        public void AddProfile() =>
            Add(IdentityResourceBuilder.Profile().Build());

        /// <summary>
        /// Adds a profile resource.
        /// </summary>
        /// <param name="configure">The <see cref="Action{IdentityResourceBuilder}"/> to configure the profile scope.</param>
        public void AddProfile(Action<IdentityResourceBuilder> configure)
        {
            var resource = IdentityResourceBuilder.Profile();
            configure(resource);
            Add(resource.Build());
        }

        /// <summary>
        /// Adds an address resource.
        /// </summary>
        public void AddAddress() =>
            Add(IdentityResourceBuilder.Address().Build());

        /// <summary>
        /// Adds an address resource.
        /// </summary>
        /// <param name="configure">The <see cref="Action{IdentityResourceBuilder}"/> to configure the address scope.</param>
        public void AddAddress(Action<IdentityResourceBuilder> configure)
        {
            var resource = IdentityResourceBuilder.Address();
            configure(resource);
            Add(resource.Build());
        }

        /// <summary>
        /// Adds an email resource.
        /// </summary>
        public void AddEmail() =>
            Add(IdentityResourceBuilder.Email().Build());

        /// <summary>
        /// Adds an email resource.
        /// </summary>
        /// <param name="configure">The <see cref="Action{IdentityResourceBuilder}"/> to configure the email scope.</param>
        public void AddEmail(Action<IdentityResourceBuilder> configure)
        {
            var resource = IdentityResourceBuilder.Email();
            configure(resource);
            Add(resource.Build());
        }

        /// <summary>
        /// Adds a phone resource.
        /// </summary>
        public void AddPhone() =>
            Add(IdentityResourceBuilder.Phone().Build());

        /// <summary>
        /// Adds a phone resource.
        /// </summary>
        /// <param name="configure">The <see cref="Action{IdentityResourceBuilder}"/> to configure the phone scope.</param>
        public void AddPhone(Action<IdentityResourceBuilder> configure)
        {
            var resource = IdentityResourceBuilder.Phone();
            configure(resource);
            Add(resource.Build());
        }
    }
}
