// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNet.Authorization;
using Microsoft.AspNet.Mvc.ApplicationModels;
using Microsoft.Framework.Internal;
using Microsoft.Framework.OptionsModel;

namespace Microsoft.AspNet.Mvc
{
    public class AuthorizationApplicationModelProvider : IApplicationModelProvider
    {
        private readonly AuthorizationOptions _authorizationOptions;

        public AuthorizationApplicationModelProvider(IOptions<AuthorizationOptions> authorizationOptionsAccessor)
        {
            _authorizationOptions = authorizationOptionsAccessor.Options;
        }

        public int Order {  get { return DefaultOrder.DefaultFrameworkSortOrder + 10; } }

        public void OnProvidersExecuted([NotNull]ApplicationModelProviderContext context)
        {
            // Intentionally empty.
        }

        public void OnProvidersExecuting([NotNull]ApplicationModelProviderContext context)
        {
            AuthorizationPolicy policy;

            foreach (var controllerModel in context.Result.Controllers)
            {
                policy = AuthorizationPolicy.Combine(
                    _authorizationOptions,
                    controllerModel.Attributes.OfType<AuthorizeAttribute>());
                if (policy != null)
                {
                    controllerModel.Filters.Add(new AuthorizeFilter(policy));
                }

                foreach (var actionModel in controllerModel.Actions)
                {
                    policy = AuthorizationPolicy.Combine(
                        _authorizationOptions,
                        actionModel.Attributes.OfType<AuthorizeAttribute>());
                    if (policy != null)
                    {
                        actionModel.Filters.Add(new AuthorizeFilter(policy));
                    }
                }
            }
        }
    }
}
