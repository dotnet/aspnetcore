// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

// -----------------------------------------------------------------------
// <copyright file="AuthenticationManager.cs" company="Microsoft">
//      Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.HttpFeature.Security;
using Microsoft.Net.Server;

namespace Microsoft.AspNet.Server.WebListener
{
    internal class AuthenticationHandler : IAuthenticationHandler
    {
        private RequestContext _requestContext;
        private AuthenticationTypes _authTypes;
        private AuthenticationTypes _customChallenges;

        internal AuthenticationHandler(RequestContext requestContext)
	    {
            _requestContext = requestContext;
            _authTypes = requestContext.AuthenticationChallenges;
            _customChallenges = AuthenticationTypes.None;
        }

        public void Authenticate(IAuthenticateContext context)
        {
            var user = _requestContext.User;
            var identity = user == null ? null : (ClaimsIdentity)user.Identity;

            foreach (var authType in ListEnabledAuthTypes())
            {
                string authString = authType.ToString();
                if (context.AuthenticationTypes.Contains(authString, StringComparer.Ordinal))
                {
                    if (identity != null && identity.IsAuthenticated
                         && string.Equals(authString, identity.AuthenticationType, StringComparison.Ordinal))
                    {
                        context.Authenticated((ClaimsIdentity)user.Identity, properties: null, description: GetDescription(user.Identity.AuthenticationType));
                    }
                    else
                    {
                        context.NotAuthenticated(authString, properties: null, description: GetDescription(user.Identity.AuthenticationType));
                    }
                }
            }
        }

        public Task AuthenticateAsync(IAuthenticateContext context)
        {
            Authenticate(context);
            return Task.FromResult(0);
        }

        public void Challenge(IChallengeContext context)
        {
            foreach (var authType in ListEnabledAuthTypes())
            {
                var authString = authType.ToString();
                // Not including any auth types means it's a blanket challenge for any auth type.
                if (context.AuthenticationTypes == null || context.AuthenticationTypes.Count == 0
                    || context.AuthenticationTypes.Contains(authString, StringComparer.Ordinal))
                {
                    _customChallenges |= authType;
                    context.Accept(authString, GetDescription(authType.ToString()));
                }
            }
            // A challenge was issued, it overrides any pre-set auth types.
            _requestContext.AuthenticationChallenges = _customChallenges;
        }

        public void GetDescriptions(IAuthTypeContext context)
        {
            // TODO: Caching, this data doesn't change per request.
            foreach (var authType in ListEnabledAuthTypes())
            {
                context.Accept(GetDescription(authType.ToString()));
            }
        }

        public void SignIn(ISignInContext context)
        {
            // Not supported
        }

        public void SignOut(ISignOutContext context)
        {
            // Not supported
        }

        private IDictionary<string, object> GetDescription(string authenticationType)
        {
            return new Dictionary<string, object>()
            {
                { "AuthenticationType", authenticationType },
                { "Caption", "Windows:" + authenticationType },
            };
        }

        private IEnumerable<AuthenticationTypes> ListEnabledAuthTypes()
        {
            // Order by strength.
            if ((_authTypes & AuthenticationTypes.Kerberos) == AuthenticationTypes.Kerberos)
            {
                yield return AuthenticationTypes.Kerberos;
            }
            if ((_authTypes & AuthenticationTypes.Negotiate) == AuthenticationTypes.Negotiate)
            {
                yield return AuthenticationTypes.Negotiate;
            }
            if ((_authTypes & AuthenticationTypes.NTLM) == AuthenticationTypes.NTLM)
            {
                yield return AuthenticationTypes.NTLM;
            }
            /*if ((_authTypes & AuthenticationTypes.Digest) == AuthenticationTypes.Digest)
            {
                // TODO:
                throw new NotImplementedException("Digest challenge generation has not been implemented.");
                yield return AuthenticationTypes.Digest;
            }*/
            if ((_authTypes & AuthenticationTypes.Basic) == AuthenticationTypes.Basic)
            {
                yield return AuthenticationTypes.Basic;
            }
        }
    }
}