// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.OptionsModel;

namespace Microsoft.AspNet.Authorization
{
    public class DefaultAuthorizationService : IAuthorizationService
    {
        private readonly IList<IAuthorizationHandler> _handlers;
        private readonly AuthorizationOptions _options;
        private readonly ILogger _logger;

        public DefaultAuthorizationService(IOptions<AuthorizationOptions> options, IEnumerable<IAuthorizationHandler> handlers, ILogger<DefaultAuthorizationService> logger)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }
            if (handlers == null)
            {
                throw new ArgumentNullException(nameof(handlers));
            }
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _handlers = handlers.ToArray();
            _options = options.Value;
            _logger = logger;
        }

        public async Task<bool> AuthorizeAsync(ClaimsPrincipal user, object resource, IEnumerable<IAuthorizationRequirement> requirements)
        {
            if (requirements == null)
            {
                throw new ArgumentNullException(nameof(requirements));
            }

            var authContext = new AuthorizationContext(requirements, user, resource);
            foreach (var handler in _handlers)
            {
                await handler.HandleAsync(authContext);
            }

            if (authContext.HasSucceeded)
            {
                _logger.LogInformation(0, "Authorization was successful for user: {userName}.", user?.Identity?.Name);
                return true;
            }
            else
            {
                _logger.LogInformation(1, "Authorization failed for user: {userName}.", user?.Identity?.Name);
                return false;
            }
        }

        public Task<bool> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
        {
            if (policyName == null)
            {
                throw new ArgumentNullException(nameof(policyName));
            }

            var policy = _options.GetPolicy(policyName);
            if (policy == null)
            {
                throw new InvalidOperationException($"No policy found: {policyName}.");
            }
            return this.AuthorizeAsync(user, resource, policy);
        }
    }
}