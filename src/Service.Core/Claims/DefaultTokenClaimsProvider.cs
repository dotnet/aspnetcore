// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Identity.Service.Claims
{
    public class DefaultTokenClaimsProvider : ITokenClaimsProvider
    {
        private readonly IOptions<IdentityServiceOptions> _options;

        public DefaultTokenClaimsProvider(IOptions<IdentityServiceOptions> options)
        {
            _options = options;
        }

        public int Order => 100;

        public Task OnGeneratingClaims(TokenGeneratingContext context)
        {
            context.AddClaimToCurrentToken(IdentityServiceClaimTypes.TokenUniqueId, Guid.NewGuid().ToString());

            var userMapping = GetUserPrincipalTokenMapping(context.CurrentToken);
            var applicationMapping = GetApplicationPrincipalTokenMapping(context.CurrentToken);
            var ambientMapping = GetAmbientClaimsTokenMapping(context.CurrentToken);

            MapFromPrincipal(context, context.User, userMapping);
            MapFromPrincipal(context, context.Application, applicationMapping);
            MapFromContext(context, context.AmbientClaims, ambientMapping);

            if (context.IsContextForTokenTypes(TokenTypes.AccessToken, TokenTypes.IdToken))
            {
                context.AddClaimToCurrentToken(IdentityServiceClaimTypes.Issuer, _options.Value.Issuer);
            }

            if (context.IsContextForTokenTypes(TokenTypes.AuthorizationCode) && context.RequestParameters.RedirectUri != null)
            {
                context.AddClaimToCurrentToken(IdentityServiceClaimTypes.RedirectUri, context.RequestParameters.RedirectUri);
            }

            return Task.CompletedTask;
        }

        private TokenMapping GetAmbientClaimsTokenMapping(string tokenType)
        {
            switch (tokenType)
            {
                case TokenTypes.AuthorizationCode:
                    return _options.Value.AuthorizationCodeOptions.ContextClaims;
                case TokenTypes.AccessToken:
                    return _options.Value.AccessTokenOptions.ContextClaims;
                case TokenTypes.IdToken:
                    return _options.Value.IdTokenOptions.ContextClaims;
                case TokenTypes.RefreshToken:
                    return _options.Value.RefreshTokenOptions.ContextClaims;
                default:
                    throw new InvalidOperationException();
            }
        }

        private TokenMapping GetApplicationPrincipalTokenMapping(string tokenType)
        {
            switch (tokenType)
            {
                case TokenTypes.AuthorizationCode:
                    return _options.Value.AuthorizationCodeOptions.ApplicationClaims;
                case TokenTypes.AccessToken:
                    return _options.Value.AccessTokenOptions.ApplicationClaims;
                case TokenTypes.IdToken:
                    return _options.Value.IdTokenOptions.ApplicationClaims;
                case TokenTypes.RefreshToken:
                    return _options.Value.RefreshTokenOptions.ApplicationClaims;
                default:
                    throw new InvalidOperationException();
            }
        }

        private TokenMapping GetUserPrincipalTokenMapping(string tokenType)
        {
            switch (tokenType)
            {
                case TokenTypes.AuthorizationCode:
                    return _options.Value.AuthorizationCodeOptions.UserClaims;
                case TokenTypes.AccessToken:
                    return _options.Value.AccessTokenOptions.UserClaims;
                case TokenTypes.IdToken:
                    return _options.Value.IdTokenOptions.UserClaims;
                case TokenTypes.RefreshToken:
                    return _options.Value.RefreshTokenOptions.UserClaims;
                default:
                    throw new InvalidOperationException();
            }
        }

        private static void MapFromPrincipal(
            TokenGeneratingContext context,
            ClaimsPrincipal user,
            TokenMapping claimsDefinition)
        {
            foreach (var mapping in claimsDefinition)
            {
                var foundClaims = user.FindAll(mapping.Alias);
                ValidateCardinality(mapping, foundClaims, claimsDefinition.Source);
                foreach (var userClaim in foundClaims)
                {
                    context.AddClaimToCurrentToken(mapping.Name, userClaim.Value);
                }
            }
        }

        private static void MapFromContext(
            TokenGeneratingContext context,
            IList<Claim> ambientClaims,
            TokenMapping claimsDefinition)
        {
            foreach (var mapping in claimsDefinition)
            {
                var ctxValues = ambientClaims.Where(c => c.Type == mapping.Alias);
                ValidateCardinality(mapping, ctxValues, claimsDefinition.Source);
                foreach (var ctxValue in ctxValues)
                {
                    context.AddClaimToCurrentToken(mapping.Name, ctxValue.Value);
                }
            }
        }

        private static void ValidateCardinality<T>(TokenValueDescriptor mapping, IEnumerable<T> foundClaims, string source)
        {
            if (mapping.Cardinality != TokenValueCardinality.Zero && !foundClaims.Any())
            {
                throw new InvalidOperationException($"Missing '{mapping.Alias}' claim from the {source}.");
            }

            if (mapping.Cardinality != TokenValueCardinality.Many && foundClaims.Skip(1).Any())
            {
                throw new InvalidOperationException($"Multiple claims found for '{mapping.Alias}' claim from the {source}.");
            }
        }
    }
}
