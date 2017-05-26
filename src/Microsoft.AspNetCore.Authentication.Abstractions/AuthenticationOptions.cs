// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Authentication
{
    public class AuthenticationOptions
    {
        private readonly IList<AuthenticationSchemeBuilder> _schemes = new List<AuthenticationSchemeBuilder>();

        /// <summary>
        /// Returns the schemes in the order they were added (important for request handling priority)
        /// </summary>
        public IEnumerable<AuthenticationSchemeBuilder> Schemes => _schemes;

        /// <summary>
        /// Maps schemes by name.
        /// </summary>
        public IDictionary<string, AuthenticationSchemeBuilder> SchemeMap { get; } = new Dictionary<string, AuthenticationSchemeBuilder>(StringComparer.Ordinal);

        /// <summary>
        /// Adds an <see cref="AuthenticationScheme"/>.
        /// </summary>
        /// <param name="name">The name of the scheme being added.</param>
        /// <param name="configureBuilder">Configures the scheme.</param>
        public void AddScheme(string name, Action<AuthenticationSchemeBuilder> configureBuilder)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (configureBuilder == null)
            {
                throw new ArgumentNullException(nameof(configureBuilder));
            }
            if (SchemeMap.ContainsKey(name))
            {
                throw new InvalidOperationException("Scheme already exists: " + name);
            }

            var builder = new AuthenticationSchemeBuilder(name);
            configureBuilder(builder);
            _schemes.Add(builder);
            SchemeMap[name] = builder;
        }

        /// <summary>
        /// Used by as the default scheme by <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.
        /// </summary>
        public string DefaultAuthenticateScheme { get; set; }

        /// <summary>
        /// Used by as the default scheme by <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.
        /// </summary>
        public string DefaultSignInScheme { get; set; }

        /// <summary>
        /// Used by as the default scheme by <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// </summary>
        public string DefaultChallengeScheme { get; set; }
    }
}
