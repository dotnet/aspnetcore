// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
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

        public int Order => -1000 + 10;

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
                var controllerModelAuthData = controllerModel.Attributes.OfType<IAuthorizeData>().ToArray();
                if (controllerModelAuthData.Length > 0)
                {
                    controllerModel.Filters.Add(GetFilter(_policyProvider, controllerModelAuthData));
                }
                foreach (var attribute in controllerModel.Attributes.OfType<IAllowAnonymous>())
                {
                    controllerModel.Filters.Add(new AllowAnonymousFilter());
                }

                foreach (var actionModel in controllerModel.Actions)
                {
                    var actionModelAuthData = actionModel.Attributes.OfType<IAuthorizeData>().ToArray();
                    if (actionModelAuthData.Length > 0)
                    {
                        actionModel.Filters.Add(GetFilter(_policyProvider, actionModelAuthData));
                    }

                    foreach (var attribute in actionModel.Attributes.OfType<IAllowAnonymous>())
                    {
                        actionModel.Filters.Add(new AllowAnonymousFilter());
                    }
                }
            }
        }

        public static AuthorizeFilter GetFilter(IAuthorizationPolicyProvider policyProvider, IEnumerable<IAuthorizeData> authData)
        {
            // The default policy provider will make the same policy for given input, so make it only once.
            // This will always execute synchronously.
            if (policyProvider.GetType() == typeof(DefaultAuthorizationPolicyProvider))
            {
                var policy = AuthorizationPolicy.CombineAsync(policyProvider, authData).GetAwaiter().GetResult();
                return new AuthorizeFilter(policy);
            }
            else
            {
                return new AuthorizeFilter(policyProvider, authData);
            }
        }
    }
}
