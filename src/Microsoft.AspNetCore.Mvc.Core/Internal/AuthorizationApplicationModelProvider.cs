// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    public class AuthorizationApplicationModelProvider : IApplicationModelProvider
    {
        private readonly IAuthorizationPolicyProvider _policyProvider;

        public AuthorizationApplicationModelProvider(IAuthorizationPolicyProvider policyProvider)
        {
            _policyProvider = policyProvider;
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

            foreach (var controllerModel in context.Result.Controllers)
            {
                controllerModel.Filters.Add(new AuthorizeFilter(_policyProvider, controllerModel.Attributes.OfType<IAuthorizeData>()));
                foreach (var attribute in controllerModel.Attributes.OfType<IAllowAnonymous>())
                {
                    controllerModel.Filters.Add(new AllowAnonymousFilter());
                }

                foreach (var actionModel in controllerModel.Actions)
                {
                    actionModel.Filters.Add(new AuthorizeFilter(_policyProvider, actionModel.Attributes.OfType<IAuthorizeData>()));
                    foreach (var attribute in actionModel.Attributes.OfType<IAllowAnonymous>())
                    {
                        actionModel.Filters.Add(new AllowAnonymousFilter());
                    }
                }
            }
        }
    }
}
