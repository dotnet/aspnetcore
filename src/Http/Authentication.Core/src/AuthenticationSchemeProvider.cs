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
        /// Creates an instance of <see cref="AuthenticationSchemeProvider"/>
        /// using the specified <paramref name="options"/>,
        /// </summary>
        /// <param name="options">The <see cref="AuthenticationOptions"/> options.</param>
        public AuthenticationSchemeProvider(IOptions<AuthenticationOptions> options)
            : this(options, new Dictionary<string, AuthenticationScheme>(StringComparer.Ordinal))
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="AuthenticationSchemeProvider"/>
        /// using the specified <paramref name="options"/> and <paramref name="schemes"/>.
        /// </summary>
        /// <param name="options">The <see cref="AuthenticationOptions"/> options.</param>
        /// <param name="schemes">The dictionary used to store authentication schemes.</param>
        protected AuthenticationSchemeProvider(IOptions<AuthenticationOptions> options, IDictionary<string, AuthenticationScheme> schemes)
        {
            _options = options.Value;

            _schemes = schemes ?? throw new ArgumentNullException(nameof(schemes));
            _requestHandlers = new List<AuthenticationScheme>();

            foreach (var builder in _options.Schemes)
            {
                var scheme = builder.Build();
                AddScheme(scheme);
            }
        }

        private readonly AuthenticationOptions _options;
        private readonly object _lock = new object();

        private readonly IDictionary<string, AuthenticationScheme> _schemes;
        private readonly List<AuthenticationScheme> _requestHandlers;
        // Used as a safe return value for enumeration apis
        private IEnumerable<AuthenticationScheme> _schemesCopy = Array.Empty<AuthenticationScheme>();
        private IEnumerable<AuthenticationScheme> _requestHandlersCopy = Array.Empty<AuthenticationScheme>();

        private Task<AuthenticationScheme> GetDefaultSchemeAsync()
            => _options.DefaultScheme != null
            ? GetSchemeAsync(_options.DefaultScheme)
            : Task.FromResult<AuthenticationScheme>(null);

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultAuthenticateScheme"/>.
        /// Otherwise, this will fallback to <see cref="AuthenticationOptions.DefaultScheme"/>.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.AuthenticateAsync(HttpContext, string)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultAuthenticateSchemeAsync()
            => _options.DefaultAuthenticateScheme != null
            ? GetSchemeAsync(_options.DefaultAuthenticateScheme)
            : GetDefaultSchemeAsync();

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultChallengeScheme"/>.
        /// Otherwise, this will fallback to <see cref="AuthenticationOptions.DefaultScheme"/>.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.ChallengeAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultChallengeSchemeAsync()
            => _options.DefaultChallengeScheme != null
            ? GetSchemeAsync(_options.DefaultChallengeScheme)
            : GetDefaultSchemeAsync();

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.ForbidAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultForbidScheme"/>.
        /// Otherwise, this will fallback to <see cref="GetDefaultChallengeSchemeAsync"/> .
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.ForbidAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultForbidSchemeAsync()
            => _options.DefaultForbidScheme != null
            ? GetSchemeAsync(_options.DefaultForbidScheme)
            : GetDefaultChallengeSchemeAsync();

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultSignInScheme"/>.
        /// Otherwise, this will fallback to <see cref="AuthenticationOptions.DefaultScheme"/>.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.SignInAsync(HttpContext, string, System.Security.Claims.ClaimsPrincipal, AuthenticationProperties)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultSignInSchemeAsync()
            => _options.DefaultSignInScheme != null
            ? GetSchemeAsync(_options.DefaultSignInScheme)
            : GetDefaultSchemeAsync();

        /// <summary>
        /// Returns the scheme that will be used by default for <see cref="IAuthenticationService.SignOutAsync(HttpContext, string, AuthenticationProperties)"/>.
        /// This is typically specified via <see cref="AuthenticationOptions.DefaultSignOutScheme"/>.
        /// Otherwise this will fallback to <see cref="GetDefaultSignInSchemeAsync"/> if that supports sign out.
        /// </summary>
        /// <returns>The scheme that will be used by default for <see cref="IAuthenticationService.SignOutAsync(HttpContext, string, AuthenticationProperties)"/>.</returns>
        public virtual Task<AuthenticationScheme> GetDefaultSignOutSchemeAsync()
            => _options.DefaultSignOutScheme != null
            ? GetSchemeAsync(_options.DefaultSignOutScheme)
            : GetDefaultSignInSchemeAsync();

        /// <summary>
        /// Returns the <see cref="AuthenticationScheme"/> matching the name, or null.
        /// </summary>
        /// <param name="name">The name of the authenticationScheme.</param>
        /// <returns>The scheme or null if not found.</returns>
        public virtual Task<AuthenticationScheme> GetSchemeAsync(string name)
            => Task.FromResult(_schemes.ContainsKey(name) ? _schemes[name] : null);

        /// <summary>
        /// Returns the schemes in priority order for request handling.
        /// </summary>
        /// <returns>The schemes in priority order for request handling</returns>
        public virtual Task<IEnumerable<AuthenticationScheme>> GetRequestHandlerSchemesAsync()
            => Task.FromResult(_requestHandlersCopy);

        /// <summary>
        /// Registers a scheme for use by <see cref="IAuthenticationService"/>. 
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        /// <returns>true if the scheme was added successfully.</returns>
        public virtual bool TryAddScheme(AuthenticationScheme scheme)
        {
            if (_schemes.ContainsKey(scheme.Name))
            {
                return false;
            }
            lock (_lock)
            {
                if (_schemes.ContainsKey(scheme.Name))
                {
                    return false;
                }
                if (typeof(IAuthenticationRequestHandler).IsAssignableFrom(scheme.HandlerType))
                {
                    _requestHandlers.Add(scheme);
                    _requestHandlersCopy = _requestHandlers.ToArray();
                }
                _schemes[scheme.Name] = scheme;
                _schemesCopy = _schemes.Values.ToArray();
                return true;
            }
        }

        /// <summary>
        /// Registers a scheme for use by <see cref="IAuthenticationService"/>. 
        /// </summary>
        /// <param name="scheme">The scheme.</param>
        public virtual void AddScheme(AuthenticationScheme scheme)
        {
            if (_schemes.ContainsKey(scheme.Name))
            {
                throw new InvalidOperationException("Scheme already exists: " + scheme.Name);
            }
            lock (_lock)
            {
                if (!TryAddScheme(scheme))
                {
                    throw new InvalidOperationException("Scheme already exists: " + scheme.Name);
                }
            }
        }

        /// <summary>
        /// Removes a scheme, preventing it from being used by <see cref="IAuthenticationService"/>.
        /// </summary>
        /// <param name="name">The name of the authenticationScheme being removed.</param>
        public virtual void RemoveScheme(string name)
        {
            if (!_schemes.ContainsKey(name))
            {
                return;
            }
            lock (_lock)
            {
                if (_schemes.ContainsKey(name))
                {
                    var scheme = _schemes[name];
                    if (_requestHandlers.Remove(scheme))
                    {
                        _requestHandlersCopy = _requestHandlers.ToArray();
                    }
                    _schemes.Remove(name);
                    _schemesCopy = _schemes.Values.ToArray();
                }
            }
        }

        public virtual Task<IEnumerable<AuthenticationScheme>> GetAllSchemesAsync()
            => Task.FromResult(_schemesCopy);
    }
}