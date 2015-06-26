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
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Http.Features.Authentication;
using Microsoft.Net.Http.Server;

namespace Microsoft.AspNet.Server.WebListener
{
    internal class AuthenticationHandler : IAuthenticationHandler
    {
        private RequestContext _requestContext;
        private AuthenticationSchemes _authSchemes;
        private AuthenticationSchemes _customChallenges;

        internal AuthenticationHandler(RequestContext requestContext)
        {
            _requestContext = requestContext;
            _authSchemes = requestContext.AuthenticationChallenges;
            _customChallenges = AuthenticationSchemes.None;
        }

        public Task AuthenticateAsync(AuthenticateContext context)
        {
            var user = _requestContext.User;
            var identity = user == null ? null : (ClaimsIdentity)user.Identity;

            foreach (var authType in ListEnabledAuthSchemes())
            {
                var authScheme = authType.ToString();
                if (string.Equals(authScheme, context.AuthenticationScheme, StringComparison.Ordinal))
                {
                    if (identity != null && identity.IsAuthenticated
                        && string.Equals(authScheme, identity.AuthenticationType, StringComparison.Ordinal))
                    {
                        context.Authenticated(new ClaimsPrincipal(user.Identity), properties: null, description: GetDescription(authScheme));
                    }
                    else
                    {
                        context.NotAuthenticated();
                    }
                }
            }
            return Task.FromResult(0);
        }

        public Task ChallengeAsync(ChallengeContext context)
        {
            foreach (var scheme in ListEnabledAuthSchemes())
            {
                var authScheme = scheme.ToString();
                // Not including any auth types means it's a blanket challenge for any auth type.
                if (context.AuthenticationScheme == string.Empty ||
                    string.Equals(context.AuthenticationScheme, authScheme, StringComparison.Ordinal))
                {
                    _requestContext.Response.StatusCode = 401;
                    _customChallenges |= scheme;
                    context.Accept();
                }
            }
            // A challenge was issued, it overrides any pre-set auth types.
            _requestContext.AuthenticationChallenges = _customChallenges;
            return Task.FromResult(0);
        }

        public void GetDescriptions(DescribeSchemesContext context)
        {
            // TODO: Caching, this data doesn't change per request.
            foreach (var scheme in ListEnabledAuthSchemes())
            {
                context.Accept(GetDescription(scheme.ToString()));
            }
        }

        public Task SignInAsync(SignInContext context)
        {
            // Not supported
            return Task.FromResult(0);
        }

        public Task SignOutAsync(SignOutContext context)
        {
            // Not supported
            return Task.FromResult(0);
        }

        private IDictionary<string, object> GetDescription(string authenticationScheme)
        {
            return new Dictionary<string, object>()
            {
                { "AuthenticationScheme", authenticationScheme },
                { "Caption", "Windows:" + authenticationScheme },
            };
        }

        private IEnumerable<AuthenticationSchemes> ListEnabledAuthSchemes()
        {
            // Order by strength.
            if ((_authSchemes & AuthenticationSchemes.Kerberos) == AuthenticationSchemes.Kerberos)
            {
                yield return AuthenticationSchemes.Kerberos;
            }
            if ((_authSchemes & AuthenticationSchemes.Negotiate) == AuthenticationSchemes.Negotiate)
            {
                yield return AuthenticationSchemes.Negotiate;
            }
            if ((_authSchemes & AuthenticationSchemes.NTLM) == AuthenticationSchemes.NTLM)
            {
                yield return AuthenticationSchemes.NTLM;
            }
            /*if ((_authSchemes & AuthenticationSchemes.Digest) == AuthenticationSchemes.Digest)
            {
                // TODO:
                throw new NotImplementedException("Digest challenge generation has not been implemented.");
                yield return AuthenticationSchemes.Digest;
            }*/
            if ((_authSchemes & AuthenticationSchemes.Basic) == AuthenticationSchemes.Basic)
            {
                yield return AuthenticationSchemes.Basic;
            }
        }
    }
}