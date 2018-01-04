// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Identity.Service
{
    internal class TokenMapper
    {
        public TokenMapper()
        {
            Claims = new List<Claim>();
        }

        public List<Claim> Claims { get; } = new List<Claim>();

        public void MapFromPrincipal(ClaimsPrincipal user, TokenMapping claimsDefinition)
        {
            foreach (var mapping in claimsDefinition)
            {
                var foundClaims = user.FindAll(mapping.Alias);
                ValidateCardinality(mapping, foundClaims, claimsDefinition.Source);
                foreach (var userClaim in foundClaims)
                {
                    Claims.Add(new Claim(mapping.Name, userClaim.Value));
                }
            }
        }

        public void MapFromContext(IList<Claim> context, TokenMapping claimsDefinition)
        {
            foreach (var mapping in claimsDefinition)
            {
                var ctxValues = context.Where(c => c.Type == mapping.Alias);
                ValidateCardinality(mapping, ctxValues, claimsDefinition.Source);
                foreach (var ctxValue in ctxValues)
                {
                    Claims.Add(new Claim(mapping.Name, ctxValue.Value));
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
