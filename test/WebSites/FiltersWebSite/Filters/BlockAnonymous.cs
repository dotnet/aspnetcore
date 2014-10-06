// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Mvc;

namespace FiltersWebSite
{
    public class BlockAnonymous : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(AuthorizationContext context)
        {
            if (!HasAllowAnonymous(context))
            {
                var user = context.HttpContext.User;
                var userIsAnonymous =
                    user == null ||
                    user.Identity == null ||
                    !user.Identity.IsAuthenticated;

                if (userIsAnonymous)
                {
                    base.Fail(context);
                }
            }
        }
    }
}