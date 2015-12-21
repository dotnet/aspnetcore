// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNet.Mvc.ApplicationModels
{
    public class AuthorizationApplicationModelProvider : IApplicationModelProvider
    {
        private readonly AuthorizationOptions _authorizationOptions;

        public AuthorizationApplicationModelProvider(IOptions<AuthorizationOptions> authorizationOptionsAccessor)
        {
            _authorizationOptions = authorizationOptionsAccessor.Value;
        }

        public int Order { get { return -1000 + 10; } }

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
            // Intentionally empty.
        }

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            AuthorizationPolicy policy;

            foreach (var controllerModel in context.Result.Controllers)
            {
                policy = AuthorizationPolicy.Combine(
                    _authorizationOptions,
                    controllerModel.Attributes.OfType<IAuthorizeData>());
                if (policy != null)
                {
                    controllerModel.Filters.Add(new AuthorizeFilter(policy));
                }

                foreach (var attribute in controllerModel.Attributes.OfType<IAllowAnonymous>())
                {
                    controllerModel.Filters.Add(new AllowAnonymousFilter());
                }

                foreach (var actionModel in controllerModel.Actions)
                {
                    policy = AuthorizationPolicy.Combine(
                        _authorizationOptions,
                        actionModel.Attributes.OfType<IAuthorizeData>());
                    if (policy != null)
                    {
                        actionModel.Filters.Add(new AuthorizeFilter(policy));
                    }

                    foreach (var attribute in actionModel.Attributes.OfType<IAllowAnonymous>())
                    {
                        actionModel.Filters.Add(new AllowAnonymousFilter());
                    }
                }
            }
        }
    }
}
