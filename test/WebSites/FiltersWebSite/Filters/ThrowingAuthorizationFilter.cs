// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Mvc.Filters;

namespace FiltersWebSite
{
    public class ThrowingAuthorizationFilter : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(AuthorizationContext context)
        {
            throw new InvalidProgramException("Authorization Filter Threw");
        }
    }
}