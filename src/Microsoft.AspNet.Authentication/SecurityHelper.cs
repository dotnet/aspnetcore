// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Http;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Helper code used when implementing authentication middleware
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Add all ClaimsIdenities from an additional ClaimPrincipal to the ClaimsPrincipal
        /// </summary>
        /// <param name="identity"></param>
        public static void AddUserPrincipal([NotNull] HttpContext context, [NotNull] ClaimsPrincipal principal)
        {
            ClaimsPrincipal existingPrincipal = context.User;
            if (existingPrincipal != null)
            {
                foreach (var existingClaimsIdentity in existingPrincipal.Identities)
                {
                    // REVIEW: No longer use auth type for anything, so we could remove this check, except for the default one HttpContext.user creates
                    // REVIEW: Need to ignore any identities that did not come from an authentication scheme?
                    if (existingClaimsIdentity.IsAuthenticated)
                    {
                        principal.AddIdentity(existingClaimsIdentity);
                    }
                }
            }
            context.User = principal;
        }
    }
}
