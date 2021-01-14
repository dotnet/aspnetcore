// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.IIS.Core
{
    /// <summary>
    /// The default authentication handler with IIS In-Process
    /// </summary>
    [Obsolete("The IISServerAuthenticationHandler is obsolete and will be removed in a future release.")]
    public class IISServerAuthenticationHandler : IAuthenticationHandler
    {
        private HttpContext? _context;
        private IISHttpContext? _iisHttpContext;

        internal AuthenticationScheme? Scheme { get; private set; }

        ///<inheritdoc/>
        public Task<AuthenticateResult> AuthenticateAsync()
        {
            Debug.Assert(_iisHttpContext != null, "Handler must be initialized.");
            Debug.Assert(Scheme != null, "Handler must be initialized.");

            var user = _iisHttpContext.WindowsUser;
            if (user != null && user.Identity != null && user.Identity.IsAuthenticated)
            {
                return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(user, Scheme.Name)));
            }
            else
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }
        }

        ///<inheritdoc/>
        public Task ChallengeAsync(AuthenticationProperties? properties)
        {
            Debug.Assert(_context != null, "Handler must be initialized.");

            // We would normally set the www-authenticate header here, but IIS does that for us.
            _context.Response.StatusCode = 401;
            return Task.CompletedTask;
        }

        ///<inheritdoc/>
        public Task ForbidAsync(AuthenticationProperties? properties)
        {
            Debug.Assert(_context != null, "Handler must be initialized.");

            _context.Response.StatusCode = 403;
            return Task.CompletedTask;
        }

        ///<inheritdoc/>
        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context)
        {
            _iisHttpContext = context.Features.Get<IISHttpContext>();
            if (_iisHttpContext == null)
            {
                throw new InvalidOperationException("No IISHttpContext found.");
            }

            Scheme = scheme;
            _context = context;

            return Task.CompletedTask;
        }
    }
}
