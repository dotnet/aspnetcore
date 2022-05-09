// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

// This is a singleton instance
// Generates strong cryptographic ids for circuits that are protected with authenticated encryption.
internal sealed class CircuitIdFactory
{
    private const string CircuitIdProtectorPurpose = "Microsoft.AspNetCore.Components.Server.CircuitIdFactory,V1";

    // We use 64 bytes, where the last 32 are the public version of the id.
    // This way we can always recover the public id from the secret form.
    private const int SecretLength = 64;
    private const int IdLength = 32;

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
        var buffer = new byte[SecretLength];
        RandomNumberGenerator.Fill(buffer);

        var id = new byte[IdLength];
        Array.Copy(
            sourceArray: buffer,
            sourceIndex: SecretLength - IdLength,
            destinationArray: id,
            destinationIndex: 0,
            length: IdLength);

        var secret = _protector.Protect(buffer);
        return new CircuitId(Base64UrlTextEncoder.Encode(secret), Base64UrlTextEncoder.Encode(id));
    }

    public bool TryParseCircuitId(string? text, out CircuitId circuitId)
    {
        if (text is null)
        {
            circuitId = default;
            return false;
        }

        try
        {
            var protectedBytes = Base64UrlTextEncoder.Decode(text);
            var unprotectedBytes = _protector.Unprotect(protectedBytes);

            if (unprotectedBytes.Length != SecretLength)
            {
                // Wrong length
                circuitId = default;
                return false;
            }

            var id = new byte[IdLength];
            Array.Copy(
                sourceArray: unprotectedBytes,
                sourceIndex: SecretLength - IdLength,
                destinationArray: id,
                destinationIndex: 0,
                length: IdLength);

            circuitId = new CircuitId(text, Base64UrlTextEncoder.Encode(id));
            return true;
        }
        catch (Exception)
        {
            // The payload format is not correct (either not base64urlencoded or not data protected)
            circuitId = default;
            return false;
        }
    }
}
