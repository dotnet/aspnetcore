// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Antiforgery
{
    /// <summary>
    /// Default implementation of <see cref="IClaimUidExtractor"/>.
    /// </summary>
    public class DefaultClaimUidExtractor : IClaimUidExtractor
    {
        private readonly ObjectPool<AntiforgerySerializationContext> _pool;

        public DefaultClaimUidExtractor(ObjectPool<AntiforgerySerializationContext> pool)
        {
            _pool = pool;
        }

        /// <inheritdoc />
        public string ExtractClaimUid(ClaimsIdentity claimsIdentity)
        {
            if (claimsIdentity == null || !claimsIdentity.IsAuthenticated)
            {
                // Skip anonymous users
                return null;
            }

            var uniqueIdentifierParameters = GetUniqueIdentifierParameters(claimsIdentity);
            var claimUidBytes = ComputeSha256(uniqueIdentifierParameters);
            return Convert.ToBase64String(claimUidBytes);
        }

        // Internal for testing
        internal static IEnumerable<string> GetUniqueIdentifierParameters(ClaimsIdentity claimsIdentity)
        {
            var nameIdentifierClaim = claimsIdentity.FindFirst(
                claim => string.Equals(ClaimTypes.NameIdentifier, claim.Type, StringComparison.Ordinal));
            if (nameIdentifierClaim != null && !string.IsNullOrEmpty(nameIdentifierClaim.Value))
            {
                return new string[]
                {
                    ClaimTypes.NameIdentifier,
                    nameIdentifierClaim.Value
                };
            }

            // We do not understand this ClaimsIdentity, fallback on serializing the entire claims Identity.
            var claims = claimsIdentity.Claims.ToList();
            claims.Sort((a, b) => string.Compare(a.Type, b.Type, StringComparison.Ordinal));

            var identifierParameters = new List<string>(claims.Count * 2);
            foreach (var claim in claims)
            {
                identifierParameters.Add(claim.Type);
                identifierParameters.Add(claim.Value);
            }

            return identifierParameters;
        }

        private byte[] ComputeSha256(IEnumerable<string> parameters)
        {
            var serializationContext = _pool.Get();

            try
            {
                var writer = serializationContext.Writer;
                foreach (string parameter in parameters)
                {
                    writer.Write(parameter); // also writes the length as a prefix; unambiguous
                }

                writer.Flush();

                var sha256 = serializationContext.Sha256;
                var stream = serializationContext.Stream;
                var bytes = sha256.ComputeHash(stream.ToArray(), 0, checked((int)stream.Length));

                return bytes;
            }
            finally
            {
                _pool.Return(serializationContext);
            }
        }
    }
}