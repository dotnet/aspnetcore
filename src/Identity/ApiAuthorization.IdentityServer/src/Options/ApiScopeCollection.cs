// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Duende.IdentityServer.Models;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer
{
    /// <summary>
    /// A collection of <see cref="ApiScope"/>.
    /// </summary>
    public class ApiScopeCollection : Collection<ApiScope>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="ApiScopeCollection"/>.
        /// </summary>
        public ApiScopeCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="ApiScopeCollection"/> with the given
        /// API scopes in <paramref name="list"/>.
        /// </summary>
        /// <param name="list">The initial list of <see cref="ApiScope"/>.</param>
        public ApiScopeCollection(IList<ApiScope> list) : base(list)
        {
        }

        /// <summary>
        /// Gets an API resource given its name.
        /// </summary>
        /// <param name="key">The name of the <see cref="ApiScope"/>.</param>
        /// <returns>The <see cref="ApiScope"/>.</returns>
        public ApiScope this[string key]
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

                throw new InvalidOperationException($"ApiScope '{key}' not found.");
            }
        }

        /// <summary>
        /// Gets whether a given scope is defined or not.
        /// </summary>
        /// <param name="key">The name of the <see cref="ApiScope"/>.</param>
        /// <returns><c>true</c> when the scope is defined; <c>false</c> otherwise.</returns>
        public bool ContainsScope(string key)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                var candidate = Items[i];
                if (string.Equals(candidate.Name, key, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds the scopes in <paramref name="scopes"/> to the collection.
        /// </summary>
        /// <param name="scopes">The list of <see cref="ApiScope"/> to add.</param>
        public void AddRange(params ApiScope[] scopes)
        {
            foreach (var resource in scopes)
            {
                Add(resource);
            }
        }
    }
}
