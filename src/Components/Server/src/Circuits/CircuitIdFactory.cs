// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
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
        public string CreateCircuitId()
        {
            var buffer = new byte[32];
            _generator.GetBytes(buffer);
            var payload = _protector.Protect(buffer);

            return Base64UrlTextEncoder.Encode(payload);
        }

        public bool ValidateCircuitId(string circuitId)
        {
            try
            {
                var protectedBytes = Base64UrlTextEncoder.Decode(circuitId);
                _protector.Unprotect(protectedBytes);

                // Its enough that we prove that we can unprotect the payload to validate the circuit id,
                // as this demonstrates that it the id wasn't tampered with.
                return true;
            }
            catch (Exception)
            {
                // The payload format is not correct (either not base64urlencoded or not data protected)
                return false;
            }
        }
    }
}
