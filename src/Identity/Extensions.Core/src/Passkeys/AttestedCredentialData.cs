// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents attested credential data in an <see cref="AuthenticatorData"/>.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#attested-credential-data"/>.
/// </remarks>
internal sealed class AttestedCredentialData
{
    /// <summary>
    /// Gets the AAGUID of the authenticator that created the credential.
    /// </summary>
    public ReadOnlyMemory<byte> Aaguid { get; }

    /// <summary>
    /// Gets the credential ID.
    /// </summary>
    public ReadOnlyMemory<byte> CredentialId { get; }

    /// <summary>
    /// Gets the credential public key.
    /// </summary>
    public CredentialPublicKey CredentialPublicKey { get; }

    private AttestedCredentialData(
        ReadOnlyMemory<byte> aaguid,
        ReadOnlyMemory<byte> credentialId,
        CredentialPublicKey credentialPublicKey)
    {
        Aaguid = aaguid;
        CredentialId = credentialId;
        CredentialPublicKey = credentialPublicKey;
    }

    public static bool TryParse(ReadOnlyMemory<byte> data, out int bytesRead, [NotNullWhen(true)] out AttestedCredentialData? result)
    {
        const int MinLength = 18; // aaguid + credential ID length
        const int MaxCredentialIdLength = 1023;

        result = null;
        bytesRead = 0;

        if (data.Length < MinLength)
        {
            return false;
        }

        var aaguid = data.Slice(0, 16);
        var credentialIDLen = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(start: 16, length: 2).Span);
        if (credentialIDLen > MaxCredentialIdLength)
        {
            return false;
        }

        var offset = 18;
        var credentialID = data.Slice(offset, credentialIDLen).ToArray();
        offset += credentialIDLen;

        var credentialPublicKey = CredentialPublicKey.Decode(data.Slice(offset), out int read);
        offset += read;

        bytesRead = offset;
        result = new AttestedCredentialData(aaguid, credentialID, credentialPublicKey);
        return true;
    }
}
