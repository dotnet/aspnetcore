// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using System.Security.Claims;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Authentication
{
    /// <summary>
    /// Helper code used when implementing authentication middleware
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Add all ClaimsIdenities from an additional ClaimPrincipal to the ClaimsPrincipal
        /// Merges a new claims principal, placing all new identities first, and eliminating
        /// any empty unauthenticated identities from context.User
        /// </summary>
        /// <param name="identity"></param>
        public static ClaimsPrincipal MergeUserPrincipal([NotNull] ClaimsPrincipal existingPrincipal, [NotNull] ClaimsPrincipal additionalPrincipal)
        {
            var newPrincipal = new ClaimsPrincipal();
            // New principal identities go first
            newPrincipal.AddIdentities(additionalPrincipal.Identities);

            // Then add any existing non empty or authenticated identities
            if (existingPrincipal != null)
            {
                newPrincipal.AddIdentities(existingPrincipal.Identities.Where(i => i.IsAuthenticated || i.Claims.Count() > 0));
            }
            return newPrincipal;
        }
    }
}
