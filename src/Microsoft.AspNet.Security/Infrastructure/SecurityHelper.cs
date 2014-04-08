// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Security.Infrastructure
{
    /// <summary>
    /// Helper code used when implementing authentication middleware
    /// </summary>
    public static class SecurityHelper
    {
        /// <summary>
        /// Add an additional ClaimsIdentity to the ClaimsPrincipal
        /// </summary>
        /// <param name="identity"></param>
        public static void AddUserIdentity([NotNull] HttpContext context, [NotNull] IIdentity identity)
        {
            var newClaimsPrincipal = new ClaimsPrincipal(identity);

            ClaimsPrincipal existingPrincipal = context.User;
            if (existingPrincipal != null)
            {
                foreach (var existingClaimsIdentity in existingPrincipal.Identities)
                {
                    if (existingClaimsIdentity.IsAuthenticated)
                    {
                        newClaimsPrincipal.AddIdentity(existingClaimsIdentity);
                    }
                }
            }
            context.User = newClaimsPrincipal;
        }

        public static bool LookupChallenge(IList<string> authenticationTypes, string authenticationType, AuthenticationMode authenticationMode)
        {
            bool challengeHasAuthenticationTypes = authenticationTypes != null && authenticationTypes.Any();
            if (!challengeHasAuthenticationTypes)
            {
                return authenticationMode == AuthenticationMode.Active;
            }
            return authenticationTypes.Contains(authenticationType, StringComparer.Ordinal);
        }

        /// <summary>
        /// Find response sign-in details for a specific authentication middleware
        /// </summary>
        /// <param name="authenticationType">The authentication type to look for</param>
        public static bool LookupSignIn(IList<ClaimsIdentity> identities, string authenticationType, out ClaimsIdentity identity)
        {
            identity = null;
            foreach (var claimsIdentity in identities)
            {
                if (string.Equals(authenticationType, claimsIdentity.AuthenticationType, StringComparison.Ordinal))
                {
                    identity = claimsIdentity;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Find response sign-out details for a specific authentication middleware
        /// </summary>
        /// <param name="authenticationType">The authentication type to look for</param>
        /// <param name="authenticationMode">The authentication mode the middleware is running under</param>
        public static bool LookupSignOut(IList<string> authenticationTypes, string authenticationType, AuthenticationMode authenticationMode)
        {
            bool singOutHasAuthenticationTypes = authenticationTypes != null && authenticationTypes.Any();
            if (!singOutHasAuthenticationTypes)
            {
                return authenticationMode == AuthenticationMode.Active;
            }
            return authenticationTypes.Contains(authenticationType, StringComparer.Ordinal);            
        }
    }
}
