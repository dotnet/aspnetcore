// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNet.Abstractions;

namespace Microsoft.AspNet.Security.Infrastructure
{
    /// <summary>
    /// Helper code used when implementing authentication middleware
    /// </summary>
    public struct SecurityHelper
    {
        private readonly HttpContext _context;

        /// <summary>
        /// Helper code used when implementing authentication middleware
        /// </summary>
        /// <param name="context"></param>
        public SecurityHelper(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            _context = context;
        }

        /// <summary>
        /// Add an additional ClaimsIdentity to the ClaimsPrincipal in the "server.User" environment key
        /// </summary>
        /// <param name="identity"></param>
        public void AddUserIdentity(IIdentity identity)
        {
            if (identity == null)
            {
                throw new ArgumentNullException("identity");
            }
            var newClaimsPrincipal = new ClaimsPrincipal(identity);

            IPrincipal existingPrincipal = _context.Request.User;
            if (existingPrincipal != null)
            {
                var existingClaimsPrincipal = existingPrincipal as ClaimsPrincipal;
                if (existingClaimsPrincipal == null)
                {
                    IIdentity existingIdentity = existingPrincipal.Identity;
                    if (existingIdentity.IsAuthenticated)
                    {
                        newClaimsPrincipal.AddIdentity(existingIdentity as ClaimsIdentity ?? new ClaimsIdentity(existingIdentity));
                    }
                }
                else
                {
                    foreach (var existingClaimsIdentity in existingClaimsPrincipal.Identities)
                    {
                        if (existingClaimsIdentity.IsAuthenticated)
                        {
                            newClaimsPrincipal.AddIdentity(existingClaimsIdentity);
                        }
                    }
                }
            }
            _context.Request.User = newClaimsPrincipal;
        }

        /// <summary>
        /// Find response challenge details for a specific authentication middleware
        /// </summary>
        /// <param name="authenticationType">The authentication type to look for</param>
        /// <param name="authenticationMode">The authentication mode the middleware is running under</param>
        /// <returns>The information instructing the middleware how it should behave</returns>
        public AuthenticationResponseChallenge LookupChallenge(string authenticationType, AuthenticationMode authenticationMode)
        {
            if (authenticationType == null)
            {
                throw new ArgumentNullException("authenticationType");
            }

            AuthenticationResponseChallenge challenge = _context.Authentication.AuthenticationResponseChallenge;
            bool challengeHasAuthenticationTypes = challenge != null && challenge.AuthenticationTypes != null && challenge.AuthenticationTypes.Length != 0;
            if (challengeHasAuthenticationTypes == false)
            {
                return authenticationMode == AuthenticationMode.Active ? (challenge ?? new AuthenticationResponseChallenge(null, null)) : null;
            }
            foreach (var challengeType in challenge.AuthenticationTypes)
            {
                if (string.Equals(challengeType, authenticationType, StringComparison.Ordinal))
                {
                    return challenge;
                }
            }
            return null;
        }

        /// <summary>
        /// Find response sign-in details for a specific authentication middleware
        /// </summary>
        /// <param name="authenticationType">The authentication type to look for</param>
        /// <returns>The information instructing the middleware how it should behave</returns>
        public AuthenticationResponseGrant LookupSignIn(string authenticationType)
        {
            if (authenticationType == null)
            {
                throw new ArgumentNullException("authenticationType");
            }

            AuthenticationResponseGrant grant = _context.Authentication.AuthenticationResponseGrant;
            if (grant == null)
            {
                return null;
            }

            foreach (var claimsIdentity in grant.Principal.Identities)
            {
                if (string.Equals(authenticationType, claimsIdentity.AuthenticationType, StringComparison.Ordinal))
                {
                    return new AuthenticationResponseGrant(claimsIdentity, grant.Properties ?? new AuthenticationProperties());
                }
            }

            return null;
        }

        /// <summary>
        /// Find response sign-out details for a specific authentication middleware
        /// </summary>
        /// <param name="authenticationType">The authentication type to look for</param>
        /// <param name="authenticationMode">The authentication mode the middleware is running under</param>
        /// <returns>The information instructing the middleware how it should behave</returns>
        public AuthenticationResponseRevoke LookupSignOut(string authenticationType, AuthenticationMode authenticationMode)
        {
            if (authenticationType == null)
            {
                throw new ArgumentNullException("authenticationType");
            }

            AuthenticationResponseRevoke revoke = _context.Authentication.AuthenticationResponseRevoke;
            if (revoke == null)
            {
                return null;
            }
            if (revoke.AuthenticationTypes == null || revoke.AuthenticationTypes.Length == 0)
            {
                return authenticationMode == AuthenticationMode.Active ? revoke : null;
            }
            for (int index = 0; index != revoke.AuthenticationTypes.Length; ++index)
            {
                if (String.Equals(authenticationType, revoke.AuthenticationTypes[index], StringComparison.Ordinal))
                {
                    return revoke;
                }
            }
            return null;
        }

        #region Value-type equality

        public bool Equals(SecurityHelper other)
        {
            return Equals(_context, other._context);
        }

        public override bool Equals(object obj)
        {
            return obj is SecurityHelper && Equals((SecurityHelper)obj);
        }

        public override int GetHashCode()
        {
            return (_context != null ? _context.GetHashCode() : 0);
        }

        public static bool operator ==(SecurityHelper left, SecurityHelper right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SecurityHelper left, SecurityHelper right)
        {
            return !left.Equals(right);
        }

        #endregion
    }
}
