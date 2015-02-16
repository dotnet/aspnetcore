// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Security
{
    public class DefaultAuthorizationService : IAuthorizationService
    {
        private readonly IList<IAuthorizationHandler> _handlers;
        private readonly AuthorizationOptions _options;

        public DefaultAuthorizationService(IOptions<AuthorizationOptions> options, IEnumerable<IAuthorizationHandler> handlers)
        {
            _handlers = handlers.ToArray();
            _options = options.Options;
        }

        public bool Authorize(ClaimsPrincipal user, object resource, string policyName)
        {
            var policy = _options.GetPolicy(policyName);
            if (policy == null)
            {
                return false;
            }
            return this.Authorize(user, resource, policy);
        }

        public bool Authorize(ClaimsPrincipal user, object resource, params IAuthorizationRequirement[] requirements)
        {
            var authContext = new AuthorizationContext(requirements, user, resource);
            foreach (var handler in _handlers)
            {
                handler.Handle(authContext);
            }
            return authContext.HasSucceeded;
        }

        public async Task<bool> AuthorizeAsync(ClaimsPrincipal user, object resource, params IAuthorizationRequirement[] requirements)
        {
            var authContext = new AuthorizationContext(requirements, user, resource);
            foreach (var handler in _handlers)
            {
                await handler.HandleAsync(authContext);
            }
            return authContext.HasSucceeded;
        }

        public Task<bool> AuthorizeAsync(ClaimsPrincipal user, object resource, string policyName)
        {
            var policy = _options.GetPolicy(policyName);
            if (policy == null)
            {
                return Task.FromResult(false);
            }
            return this.AuthorizeAsync(user, resource, policy);
        }
    }
}