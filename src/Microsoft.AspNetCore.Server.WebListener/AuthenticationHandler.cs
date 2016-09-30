// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.Internal;
using Microsoft.Net.Http.Server;

namespace Microsoft.AspNetCore.Server.WebListener
{
    internal class AuthenticationHandler : IAuthenticationHandler
    {
        private RequestContext _requestContext;
        private AuthenticationSchemes _authSchemes;
        private AuthenticationSchemes _customChallenges;

        internal AuthenticationHandler(RequestContext requestContext)
        {
            _requestContext = requestContext;
            _authSchemes = requestContext.Response.AuthenticationChallenges;
            _customChallenges = AuthenticationSchemes.None;
        }

        public Task AuthenticateAsync(AuthenticateContext context)
        {
            var identity = (ClaimsIdentity)_requestContext.User?.Identity;

            foreach (var authType in ListEnabledAuthSchemes())
            {
                var authScheme = authType.ToString();
                if (string.Equals(authScheme, context.AuthenticationScheme, StringComparison.Ordinal))
                {
                    if (identity != null && identity.IsAuthenticated
                        && string.Equals(authScheme, identity.AuthenticationType, StringComparison.Ordinal))
                    {
                        context.Authenticated(new ClaimsPrincipal(identity), properties: null, description: GetDescription(authScheme));
                    }
                    else
                    {
                        context.NotAuthenticated();
                    }
                }
            }
            return TaskCache.CompletedTask;
        }

        public Task ChallengeAsync(ChallengeContext context)
        {
            var automaticChallenge = string.Equals("Automatic", context.AuthenticationScheme, StringComparison.Ordinal);
            foreach (var scheme in ListEnabledAuthSchemes())
            {
                var authScheme = scheme.ToString();
                // Not including any auth types means it's a blanket challenge for any auth type.
                if (automaticChallenge || string.Equals(context.AuthenticationScheme, authScheme, StringComparison.Ordinal))
                {
                    switch (context.Behavior)
                    {
                        case ChallengeBehavior.Forbidden:
                            _requestContext.Response.StatusCode = 403;
                            context.Accept();
                            break;
                        case ChallengeBehavior.Unauthorized:
                            _requestContext.Response.StatusCode = 401;
                            _customChallenges |= scheme;
                            context.Accept();
                            break;
                        case ChallengeBehavior.Automatic:
                            var identity = (ClaimsIdentity)_requestContext.User?.Identity;
                            if (identity != null && identity.IsAuthenticated
                                && (automaticChallenge || string.Equals(identity.AuthenticationType, context.AuthenticationScheme, StringComparison.Ordinal)))
                            {
                                _requestContext.Response.StatusCode = 403;
                                context.Accept();
                            }
                            else
                            {
                                _requestContext.Response.StatusCode = 401;
                                _customChallenges |= scheme;
                                context.Accept();
                            }
                            break;
                        default:
                            throw new NotSupportedException(context.Behavior.ToString());
                    }
                }
            }
            // A challenge was issued, it overrides any pre-set auth types.
            _requestContext.Response.AuthenticationChallenges = _customChallenges;
            return TaskCache.CompletedTask;
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
            // Not supported. AuthenticationManager will throw if !Accepted.
            return TaskCache.CompletedTask;
        }

        public Task SignOutAsync(SignOutContext context)
        {
            // Not supported. AuthenticationManager will throw if !Accepted.
            return TaskCache.CompletedTask;
        }

        private IDictionary<string, object> GetDescription(string authenticationScheme)
        {
            return new Dictionary<string, object>()
            {
                { "AuthenticationScheme", authenticationScheme },
                { "DisplayName", "Windows:" + authenticationScheme },
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