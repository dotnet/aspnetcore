// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Encodes contextual bindings made by an authenticator.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#authenticator-data"/>
/// </remarks>
internal sealed class AuthenticatorData(
    ReadOnlyMemory<byte> rpIdHash,
    AuthenticatorDataFlags flags,
    uint signCount,
    AttestedCredentialData? attestedCredentialData,
    ReadOnlyMemory<byte>? extensions)
{
    private readonly AuthenticatorDataFlags _flags = flags;

    /// <summary>
    /// Gets the SHA-256 hash of the Relying Party ID the credential is scoped to.
    /// </summary>
    public ReadOnlyMemory<byte> RpIdHash { get; } = rpIdHash;

    /// <summary>
    /// Gets the signature counter.
    /// </summary>
    public uint SignCount { get; } = signCount;

    /// <summary>
    /// Gets the attested credential data.
    /// </summary>
    public AttestedCredentialData? AttestedCredentialData { get; } = attestedCredentialData;

    /// <summary>
    /// Gets the extension-defined authenticator data.
    /// </summary>
    public ReadOnlyMemory<byte>? Extensions { get; } = extensions;

    /// <summary>
    /// Gets the flags for this authenticator data.
    /// </summary>
    public AuthenticatorDataFlags Flags => _flags;

    /// <summary>
    /// Gets whether the user is present.
    /// </summary>
    public bool IsUserPresent => _flags.HasFlag(AuthenticatorDataFlags.UserPresent);

    /// <summary>
    /// Gets whether the user is verified.
    /// </summary>
    public bool IsUserVerified => _flags.HasFlag(AuthenticatorDataFlags.UserVerified);

    /// <summary>
    /// Gets whether the public key credential source is backup eligible.
    /// </summary>
    public bool IsBackupEligible => _flags.HasFlag(AuthenticatorDataFlags.BackupEligible);

    /// <summary>
    /// Gets whether the public key credential source is currently backed up.
    /// </summary>
    public bool IsBackedUp => _flags.HasFlag(AuthenticatorDataFlags.BackedUp);

    /// <summary>
    /// Gets whether the authenticator data has extensions.
    /// </summary>
    public bool HasExtensionsData => _flags.HasFlag(AuthenticatorDataFlags.HasExtensionData);

    /// <summary>
    /// Gets whether the authenticator added attested credential data.
    /// </summary>
    public bool HasAttestedCredentialData => _flags.HasFlag(AuthenticatorDataFlags.HasAttestedCredentialData);

    public static bool TryParse(ReadOnlyMemory<byte> bytes, [NotNullWhen(true)] out AuthenticatorData? result)
    {
        // Min length specified in https://www.w3.org/TR/webauthn-3/#authenticator-data
        const int MinLength = 37;

        if (bytes.Length < MinLength)
        {
            result = null;
            return false;
        }

        var offset = 0;
        var rpIdHash = ReadBytes(32);
        var flags = (AuthenticatorDataFlags)ReadByte();
        var signCount = BinaryPrimitives.ReadUInt32BigEndian(ReadBytes(4).Span);

        AttestedCredentialData? attestedCredentialData = null;
        if (flags.HasFlag(AuthenticatorDataFlags.HasAttestedCredentialData))
        {
            var remaining = bytes.Slice(offset);
            if (!AttestedCredentialData.TryParse(remaining, out var bytesRead, out attestedCredentialData))
            {
                result = null;
                return false;
            }
            offset += bytesRead;
        }

        ReadOnlyMemory<byte>? extensions = default;
        if (flags.HasFlag(AuthenticatorDataFlags.HasExtensionData))
        {
            var reader = new CborReader(bytes.Slice(offset));
            extensions = reader.ReadEncodedValue();
            offset += extensions.Value.Length;
        }

        if (offset != bytes.Length)
        {
            // Leftover bytes signifies a possible parsing error.
            result = null;
            return false;
        }

        result = new(rpIdHash, flags, signCount, attestedCredentialData, extensions);
        return true;

        byte ReadByte() => bytes.Span[offset++];

        ReadOnlyMemory<byte> ReadBytes(int length)
        {
            var result = bytes.Slice(offset, length);
            offset += length;
            return result;
        }
    }
}
