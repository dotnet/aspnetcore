// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;

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
    /// Gets or sets the AAGUID of the authenticator that created the credential.
    /// </summary>
    public required ReadOnlyMemory<byte> Aaguid { get; init; }

    /// <summary>
    /// Gets or sets the credential ID.
    /// </summary>
    public required ReadOnlyMemory<byte> CredentialId { get; init; }

    /// <summary>
    /// Gets or sets the credential public key.
    /// </summary>
    public required CredentialPublicKey CredentialPublicKey { get; init; }

    public static AttestedCredentialData Parse(ReadOnlyMemory<byte> data, out int bytesRead)
    {
        try
        {
            return ParseCore(data, out bytesRead);
        }
        catch (PasskeyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw PasskeyException.InvalidAttestedCredentialDataFormat(ex);
        }
    }

    private static AttestedCredentialData ParseCore(ReadOnlyMemory<byte> data, out int bytesRead)
    {
        const int AaguidLength = 16;
        const int CredentialIdLengthLength = 2;
        const int MinLength = AaguidLength + CredentialIdLengthLength;
        const int MaxCredentialIdLength = 1023;

        var offset = 0;

        if (data.Length < MinLength)
        {
            throw PasskeyException.InvalidAttestedCredentialDataLength(data.Length);
        }

        var aaguid = data.Slice(offset, AaguidLength);
        offset += AaguidLength;

        var credentialIdLength = BinaryPrimitives.ReadUInt16BigEndian(data.Slice(offset, CredentialIdLengthLength).Span);
        offset += CredentialIdLengthLength;

        if (credentialIdLength > MaxCredentialIdLength)
        {
            throw PasskeyException.InvalidCredentialIdLength(credentialIdLength);
        }

        var credentialId = data.Slice(offset, credentialIdLength).ToArray();
        offset += credentialIdLength;

        var credentialPublicKey = CredentialPublicKey.Decode(data[offset..], out var read);
        offset += read;

        bytesRead = offset;
        return new()
        {
            Aaguid = aaguid,
            CredentialId = credentialId,
            CredentialPublicKey = credentialPublicKey,
        };
    }
}
