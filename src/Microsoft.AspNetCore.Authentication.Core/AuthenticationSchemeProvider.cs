// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/>.
        /// Otherwise, if only a single scheme exists, that will be used, if more than one exists, null will be returned.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.</returns>
        public Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync()
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
        /// Otherwise, if only a single scheme exists, that will be used, if more than one exists, null will be returned.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        public Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync()
        {
            if (_options.DefaultChallengeScheme != null)
            {
                return GetSchemeAsync(_options.DefaultChallengeScheme);
            }
            if (_map.Count == 1)
            {
                return Task.FromResult(_map.Values.First());
            }
            return Task.FromResult<AuthenticationScheme>(null);
        }

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultSignInScheme"/>.
        /// Otherwise, if only a single scheme exists, that will be used, if more than one exists, null will be returned.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.</returns>
        public Task<AuthenticationScheme> GetDefaultSignInSchemeAsync()
        {
            if (_options.DefaultSignInScheme != null)
            {
                return GetSchemeAsync(_options.DefaultSignInScheme);
            }
            if (_map.Count == 1)
            {
                return Task.FromResult(_map.Values.First());
            }
            return Task.FromResult<AuthenticationScheme>(null);
        }

        /// <summary>
        /// Returns the <see cref="AuthenticationScheme"/> matching the name, or null.
        /// </summary>
        /// <param name="name">The name of the authenticationScheme.</param>
        /// <returns>The scheme or null if not found.</returns>
        public Task<AuthenticationScheme> GetSchemeAsync(string name)
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
        public Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
        {
            return Task.FromResult<IEnumerable<AuthenticationScheme>>(_requestHandlers);
        }

        /// <summary>
        /// Registers a scheme for use by <see cref="IAuthenticationService"/>. 
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        public void AddScheme(AuthenticationScheme scheme)
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
                _map[scheme.Name] = scheme;
            }
        }

        /// <summary>
        /// Removes a scheme, preventing it from being used by <see cref="IAuthenticationService"/>.
        /// </summary>
        /// <param name="name">The name of the authenticationScheme being removed.</param>
        public void RemoveScheme(string name)
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
                    _requestHandlers.Remove(_requestHandlers.Where(s => s.Name == name).FirstOrDefault());
                    _map.Remove(name);
                }
            }
        }

        public Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
        {
            return Task.FromResult<IEnumerable<AuthenticationScheme>>(_map.Values);
        }
    }
}