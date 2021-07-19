// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Duende.IdentityServer.Models;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    /// <summary>
    /// A collection of <see cref="ApiResource"/>.
    /// </summary>
    public class ApiResourceCollection : Collection<ApiResource>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApiResourceCollection"/>.
        /// </summary>
        public ApiResourceCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ApiResourceCollection"/> with the given
        /// API resources in <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The initial list of <see cref="ApiResource"/>.</param>
        public ApiResourceCollection(IList<ApiResource> list) : base(list)
        {
        }

        /// <summary>
        /// Gets an API resource given its name.
        /// </summary>
        /// <param name="key">The name of the <see cref="ApiResource"/>.</param>
        /// <returns>The <see cref="ApiResource"/>.</returns>
        public ApiResource this[string key]
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

                throw new InvalidOperationException($"ApiResource '{key}' not found.");
            }
        }

        /// <summary>
        /// Adds the resources in <paramref name="resources"/> to the collection.
        /// </summary>
        /// <param name="resources">The list of <see cref="ApiResource"/> to add.</param>
        public void AddRange(params ApiResource[] resources)
        {
            foreach (var resource in resources)
            {
                Add(resource);
            }
        }

        /// <summary>
        /// Adds a new externally registered API.
        /// </summary>
        /// <param name="name">The name of the API.</param>
        /// <param name="configure">The <see cref="Action{ApiResourceBuilder}"/> to configure the externally registered API.</param>
        public void AddApiResource(string name, Action<ApiResourceBuilder> configure)
        {
            var apiResource = ApiResourceBuilder.ApiResource(name);
            configure(apiResource);
            Add(apiResource.Build());
        }

        /// <summary>
        /// Creates a new API that coexists with an authorization server.
        /// </summary>
        /// <param name="name">The name of the API.</param>
        /// <param name="configure">The <see cref="Func{ApiResourceBuilder, ApiResource}"/> to configure the identity server jwt API.</param>
        public void AddIdentityServerJwt(string name, Action<ApiResourceBuilder> configure)
        {
            var apiResource = ApiResourceBuilder.IdentityServerJwt(name);
            configure(apiResource);
            Add(apiResource.Build());
        }
    }
}
