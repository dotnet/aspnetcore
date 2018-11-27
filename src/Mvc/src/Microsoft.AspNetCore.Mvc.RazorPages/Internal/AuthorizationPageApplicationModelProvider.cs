// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Internal;

namespace Microsoft.AspNetCore.Mvc.RazorPages.Internal
{
    public class AuthorizationPageApplicationModelProvider : IPageApplicationModelProvider
    {
        private readonly IAuthorizationPolicyProvider _policyProvider;

        public AuthorizationPageApplicationModelProvider(IAuthorizationPolicyProvider policyProvider)
        {
            _policyProvider = policyProvider;
        }

        // The order is set to execute after the DefaultPageApplicationModelProvider.
        public int Order => -1000 + 10;

        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var pageModel = context.PageApplicationModel;
            var authorizeData = pageModel.HandlerTypeAttributes.OfType<IAuthorizeData>().ToArray();
            if (authorizeData.Length > 0)
            {
                pageModel.Filters.Add(AuthorizationApplicationModelProvider.GetFilter(_policyProvider, authorizeData));
            }
            foreach (var attribute in pageModel.HandlerTypeAttributes.OfType<IAllowAnonymous>())
            {
                pageModel.Filters.Add(new AllowAnonymousFilter());
            }
        }

        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }
    }
}
