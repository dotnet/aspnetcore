// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
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
        /// Adds an <see cref="AuthenticationScheme"/>.
        /// </summary>
        /// <typeparam name="THandler">The <see cref="IAuthenticationHandler"/> responsible for the scheme.</typeparam>
        /// <param name="name">The name of the scheme being added.</param>
        /// <param name="displayName">The display name for the scheme.</param>
        public void AddScheme<THandler>(string name, string displayName) where THandler : IAuthenticationHandler
            => AddScheme(name, b =>
            {
                b.DisplayName = displayName;
                b.HandlerType = typeof(THandler);
            });

        /// <summary>
        /// Used as the fallback default scheme for all the other defaults.
        /// </summary>
        public string DefaultScheme { get; set; }

        /// <summary>
        /// Used as the default scheme by <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.
        /// </summary>
        public string DefaultAuthenticateScheme { get; set; }

        /// <summary>
        /// Used as the default scheme by <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.
        /// </summary>
        public string DefaultSignInScheme { get; set; }

        /// <summary>
        /// Used as the default scheme by <see cref="IAuthenticationService.SignOutAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// </summary>
        public string DefaultSignOutScheme { get; set; }

        /// <summary>
        /// Used as the default scheme by <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// </summary>
        public string DefaultChallengeScheme { get; set; }

        /// <summary>
        /// Used as the default scheme by <see cref="IAuthenticationService.ForbidAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// </summary>
        public string DefaultForbidScheme { get; set; }

        /// <summary>
        /// If true, SignIn should throw if attempted with a ClaimsPrincipal.Identity.IsAuthenticated = false.
        /// </summary>
        public bool RequireAuthenticatedSignIn { get; set; } = true;
    }
}
