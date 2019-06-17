// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Circuits
{
    // This is a singleton instance
    // Generates strong cryptographic ids for circuits that are protected with authenticated encryption.
    internal class CircuitIdFactory
    {
        private const string CircuitIdProtectorPurpose = "Microsoft.AspNetCore.Components.Server";

        private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();
        private readonly IDataProtector _protector;

        public CircuitIdFactory(IDataProtectionProvider provider)
        {
            _protector = provider.CreateProtector(CircuitIdProtectorPurpose);
        }

        // Generates a circuit id that is produced from a strong cryptographic random number generator
        // we don't care about the underlying payload, other than its uniqueness and the fact that we
        // authenticate encrypt it using data protection.
        // For validation, the fact that we can unprotect the payload is guarantee enough.
        public CircuitId CreateCircuitId()
        {
            var buffer = new byte[32];
            _generator.GetBytes(buffer);
            var payload = _protector.Protect(buffer);

            return new CircuitId(buffer, GetHash(buffer), payload);
        }

        private static byte[] GetHash(byte[] buffer)
        {
            var hashAlgorithm = SHA256.Create();
            var hash = hashAlgorithm.ComputeHash(buffer);
            return hash;
        }

        public bool ValidateCircuitId(string circuitId, string cookie, out CircuitId id)
        {
            id = FromCookieValue(cookie);
            var rawRequestId = Base64UrlTextEncoder.Decode(circuitId);
            var requestTokenBytes = Base64UrlTextEncoder.Decode(id.RequestToken);
            if (CryptographicOperations.FixedTimeEquals(rawRequestId, requestTokenBytes))
            {
                return true;
            }
            else
            {
                id = default;
                return false;
            }
        }

        public bool ValidateCircuitId(string circuitId, ClaimsPrincipal user)
        {
            try
            {
                foreach (var claim in user.Claims)
                {
                    if (claim.Type.Equals(CircuitAuthenticationHandler.IdClaimType))
                    {
                        var rawRequestId = Base64UrlTextEncoder.Decode(claim.Value);
                        var requestTokenBytes = Base64UrlTextEncoder.Decode(circuitId);

                        if (CryptographicOperations.FixedTimeEquals(rawRequestId, requestTokenBytes))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception)
            {
                // The payload format is not correct (either not base64urlencoded or not data protected)
                return false;
            }
        }

        internal CircuitId FromCookieValue(string value)
        {
            var payload = Base64UrlTextEncoder.Decode(value);
            var id = _protector.Unprotect(payload);
            return new CircuitId(id, GetHash(id), payload);
        }
    }

    internal struct CircuitId
    {
        public CircuitId(byte[] id, byte[] hash, byte[] payload) : this()
        {
            Id = id;
            RequestToken = Base64UrlTextEncoder.Encode(hash);
            CookieToken = Base64UrlTextEncoder.Encode(payload);
        }

        public byte[] Id { get; }

        public string RequestToken { get; }

        public string CookieToken { get; }
    }
}
