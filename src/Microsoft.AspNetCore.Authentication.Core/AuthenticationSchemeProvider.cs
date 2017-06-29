// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Implements <see cref="IAuthenticationSchemeProvider"/>.
    /// </summary>
    public class AuthenticationSchemeProvider : IAuthenticationSchemeProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="options">The <see cref="AuthenticationOptions"/> options.</param>
        public AuthenticationSchemeProvider(IOptions<AuthenticationOptions> options)
        {
            _options = options.Value;

            foreach (var builder in _options.Schemes)
            {
                var scheme = builder.Build();
                AddScheme(scheme);
            }
        }

        private readonly AuthenticationOptions _options;
        private readonly object _lock = new object();

        private IDictionary<string, AuthenticationScheme> _map = new Dictionary<string, AuthenticationScheme>(StringComparer.Ordinal);
        private List<AuthenticationScheme> _requestHandlers = new List<AuthenticationScheme>();
        private List<AuthenticationScheme> _signOutHandlers = new List<AuthenticationScheme>();
        private List<AuthenticationScheme> _signInHandlers = new List<AuthenticationScheme>();

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/>.
        /// Otherwise, if only a single scheme exists, that will be used, if more than one exists, null will be returned.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync()
        {
            if (_options.DefaultAuthenticateScheme != null)
            {
                return GetSchemeAsync(_options.DefaultAuthenticateScheme);
            }
            if (_map.Count == 1)
            {
                return Task.FromResult(_map.Values.First());
            }
            return Task.FromResult<AuthenticationScheme>(null);
        }

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultChallengeScheme"/>.
        /// Otherwise, this will fallback to <see cref="GetDefaultAuthenticateSchemeAsync"/>.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync()
        {
            if (_options.DefaultChallengeScheme != null)
            {
                return GetSchemeAsync(_options.DefaultChallengeScheme);
            }
            return GetDefaultAuthenticateSchemeAsync();
        }

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.ForbidAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultForbidScheme"/>.
        /// Otherwise, this will fallback to <see cref="GetDefaultChallengeSchemeAsync"/> .
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.ForbidAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultForbidSchemeAsync()
        {
            if (_options.DefaultForbidScheme != null)
            {
                return GetSchemeAsync(_options.DefaultForbidScheme);
            }
            return GetDefaultChallengeSchemeAsync();
        }

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultSignInScheme"/>.
        /// If only a single sign in handler scheme exists, that will be used, if more than one exists,
        /// this will fallback to <see cref="GetDefaultAuthenticateSchemeAsync"/>.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultSignInSchemeAsync()
        {
            if (_options.DefaultSignInScheme != null)
            {
                return GetSchemeAsync(_options.DefaultSignInScheme);
            }
            if (_signInHandlers.Count == 1)
            {
                return Task.FromResult(_signInHandlers[0]);
            }
            return GetDefaultAuthenticateSchemeAsync();
        }

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.SignOutAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultSignOutScheme"/>.
        /// If only a single sign out handler scheme exists, that will be used, if more than one exists,
        /// this will fallback to <see cref="GetDefaultSignInSchemeAsync"/> if that supoorts sign out.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.SignOutAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync()
        {
            if (_options.DefaultSignOutScheme != null)
            {
                return GetSchemeAsync(_options.DefaultSignOutScheme);
            }
            if (_signOutHandlers.Count == 1)
            {
                return Task.FromResult(_signOutHandlers[0]);
            }
            return GetDefaultSignInSchemeAsync();
        }

        /// <summary>
        /// Returns the <see cref="AuthenticationScheme"/> matching the name, or null.
        /// </summary>
        /// <param name="name">The name of the authenticationScheme.</param>
        /// <returns>The scheme or null if not found.</returns>
        public virtual Task<AuthenticationScheme> GetSchemeAsync(string name)
        {
            if (_map.ContainsKey(name))
            {
                return Task.FromResult(_map[name]);
            }
            return Task.FromResult<AuthenticationScheme>(null);
        }

        /// <summary>
        /// Returns the schemes in priority order for request handling.
        /// </summary>
        /// <returns>The schemes in priority order for request handling</returns>
        public virtual Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
        {
            return Task.FromResult<IEnumerable<AuthenticationScheme>>(_requestHandlers);
        }

        /// <summary>
        /// Registers a scheme for use by <see cref="IAuthenticationService"/>. 
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        public virtual void AddScheme(AuthenticationScheme scheme)
        {
            if (_map.ContainsKey(scheme.Name))
            {
                throw new InvalidOperationException("Scheme already exists: " + scheme.Name);
            }
            lock (_lock)
            {
                if (_map.ContainsKey(scheme.Name))
                {
                    throw new InvalidOperationException("Scheme already exists: " + scheme.Name);
                }
                if (typeof(IAuthenticationRequestHandler).IsAssignableFrom(scheme.HandlerType))
                {
                    _requestHandlers.Add(scheme);
                }
                if (typeof(IAuthenticationSignInHandler).IsAssignableFrom(scheme.HandlerType))
                {
                    _signInHandlers.Add(scheme);
                }
                if (typeof(IAuthenticationSignOutHandler).IsAssignableFrom(scheme.HandlerType))
                {
                    _signOutHandlers.Add(scheme);
                }
                _map[scheme.Name] = scheme;
            }
        }

        /// <summary>
        /// Removes a scheme, preventing it from being used by <see cref="IAuthenticationService"/>.
        /// </summary>
        /// <param name="name">The name of the authenticationScheme being removed.</param>
        public virtual void RemoveScheme(string name)
        {
            if (!_map.ContainsKey(name))
            {
                return;
            }
            lock (_lock)
            {
                if (_map.ContainsKey(name))
                {
                    var scheme = _map[name];
                    _requestHandlers.Remove(scheme);
                    _signInHandlers.Remove(scheme);
                    _signOutHandlers.Remove(scheme);
                    _map.Remove(name);
                }
            }
        }

        public virtual Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
            => Task.FromResult<IEnumerable<AuthenticationScheme>>(_map.Values);
    }
}