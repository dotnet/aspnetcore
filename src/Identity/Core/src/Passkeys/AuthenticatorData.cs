// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Encodes contextual bindings made by an authenticator.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#authenticator-data"/>
/// </remarks>
internal sealed class AuthenticatorData
{
    /// <summary>
    /// Gets or sets the SHA-256 hash of the Relying Party ID the credential is scoped to.
    /// </summary>
    public required ReadOnlyMemory<byte> RpIdHash { get; init; }

    /// <summary>
    /// Gets or sets the flags for this authenticator data.
    /// </summary>
    public required AuthenticatorDataFlags Flags { get; init; }

    /// <summary>
    /// Gets or sets the signature counter.
    /// </summary>
    public required uint SignCount { get; init; }

    /// <summary>
    /// Gets or sets the attested credential data.
    /// </summary>
    public AttestedCredentialData? AttestedCredentialData { get; init; }

    /// <summary>
    /// Gets or sets the extension-defined authenticator data.
    /// </summary>
    public ReadOnlyMemory<byte>? Extensions { get; init; }

    /// <summary>
    /// Gets whether the user is present.
    /// </summary>
    public bool IsUserPresent => Flags.HasFlag(AuthenticatorDataFlags.UserPresent);

    /// <summary>
    /// Gets whether the user is verified.
    /// </summary>
    public bool IsUserVerified => Flags.HasFlag(AuthenticatorDataFlags.UserVerified);

    /// <summary>
    /// Gets whether the public key credential source is backup eligible.
    /// </summary>
    public bool IsBackupEligible => Flags.HasFlag(AuthenticatorDataFlags.BackupEligible);

    /// <summary>
    /// Gets whether the public key credential source is currently backed up.
    /// </summary>
    public bool IsBackedUp => Flags.HasFlag(AuthenticatorDataFlags.BackedUp);

    /// <summary>
    /// Gets whether the authenticator data has extensions.
    /// </summary>
    public bool HasExtensionsData => Flags.HasFlag(AuthenticatorDataFlags.HasExtensionData);

    /// <summary>
    /// Gets whether the authenticator added attested credential data.
    /// </summary>
    [MemberNotNullWhen(true, nameof(AttestedCredentialData))]
    public bool HasAttestedCredentialData => Flags.HasFlag(AuthenticatorDataFlags.HasAttestedCredentialData);

    public static AuthenticatorData Parse(ReadOnlyMemory<byte> bytes)
    {
        try
        {
            return ParseCore(bytes);
        }
        catch (PasskeyException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw PasskeyException.InvalidAuthenticatorDataFormat(ex);
        }
    }

    private static AuthenticatorData ParseCore(ReadOnlyMemory<byte> bytes)
    {
        const int RpIdHashLength = 32;
        const int AuthenticatorDataFlagsLength = 1;
        const int SignCountLength = 4;
        const int MinLength = RpIdHashLength + AuthenticatorDataFlagsLength + SignCountLength;

        // Min length specified in https://www.w3.org/TR/webauthn-3/#authenticator-data
        Debug.Assert(MinLength == 37);
        if (bytes.Length < MinLength)
        {
            throw PasskeyException.InvalidAuthenticatorDataLength(bytes.Length);
        }

        var offset = 0;

        var rpIdHash = bytes.Slice(offset, RpIdHashLength);
        offset += RpIdHashLength;

        var flags = (AuthenticatorDataFlags)bytes.Span[offset];
        offset += AuthenticatorDataFlagsLength;

        var signCount = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset, SignCountLength).Span);
        offset += SignCountLength;

        AttestedCredentialData? attestedCredentialData = null;
        if (flags.HasFlag(AuthenticatorDataFlags.HasAttestedCredentialData))
        {
            var remaining = bytes[offset..];
            attestedCredentialData = AttestedCredentialData.Parse(remaining, out var bytesRead);
            offset += bytesRead;
        }

        ReadOnlyMemory<byte>? extensions = default;
        if (flags.HasFlag(AuthenticatorDataFlags.HasExtensionData))
        {
            var reader = new CborReader(bytes[offset..]);
            extensions = reader.ReadEncodedValue();
            offset += extensions.Value.Length;
        }

        if (offset != bytes.Length)
        {
            // Leftover bytes signifies a possible parsing error.
            throw PasskeyException.InvalidAuthenticatorDataFormat();
        }

        return new()
        {
            RpIdHash = rpIdHash,
            Flags = flags,
            SignCount = signCount,
            AttestedCredentialData = attestedCredentialData,
            Extensions = extensions,
        };
    }
}
