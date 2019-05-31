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
    // Generates strong criptographic ids for circuits that are protected with authenticated encryption.
    internal class CircuitIdFactory
    {
        // Follow up with blowdart/levi. Create should be good enough, but not sure if we need
        // to do anything additional for FIPS compliance.
        private readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();
        private readonly IDataProtector _protector;

        public CircuitIdFactory(IOptions<CircuitOptions> options)
        {
            _protector = options.Value.CircuitIdProtector;
        }

        // Generates a circuit id that is produced from a strong cryptographic random number generator
        // we don't care about the underlying payload, other than its uniqueness and the fact that we
        // authenticate encrypt it using data protection.
        // For validation, the fact that we can unprotect the payload is guarantee enough.
        public string CreateCircuitId()
        {
            var buffer = new byte[32];
            try
            {
                _generator.GetBytes(buffer);
                var payload = _protector.Protect(buffer);

                return Base64UrlTextEncoder.Encode(payload);
            }
            finally
            {
                // Its good practice to clear secrets from memory as soon as we are done with them.
                Array.Clear(buffer, 0, 32);
            }
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
