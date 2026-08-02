// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Formats.Cbor;

namespace Microsoft.AspNetCore.Identity.Test;

internal static class CredentialHelpers
{
    public static ReadOnlyMemory<byte> MakeAttestedCredentialData(in AttestedCredentialDataArgs args)
    {
        const int AaguidLength = 16;
        const int CredentialIdLengthLength = 2;
        var length = AaguidLength + CredentialIdLengthLength + args.CredentialId.Length + args.CredentialPublicKey.Length;
        var result = new byte[length];
        var offset = 0;

        args.Aaguid.Span.CopyTo(result.AsSpan(offset, AaguidLength));
        offset += AaguidLength;

        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset, CredentialIdLengthLength), (ushort)args.CredentialId.Length);
        offset += CredentialIdLengthLength;

        args.CredentialId.Span.CopyTo(result.AsSpan(offset));
        offset += args.CredentialId.Length;

        args.CredentialPublicKey.Span.CopyTo(result.AsSpan(offset));
        offset += args.CredentialPublicKey.Length;

        if (offset != result.Length)
        {
            throw new InvalidOperationException($"Expected attested credential data length '{length}', but got '{offset}'.");
        }

        return result;
    }

    public static ReadOnlyMemory<byte> MakeAuthenticatorData(in AuthenticatorDataArgs args)
    {
        const int RpIdHashLength = 32;
        const int AuthenticatorDataFlagsLength = 1;
        const int SignCountLength = 4;
        var length =
            RpIdHashLength +
            AuthenticatorDataFlagsLength +
            SignCountLength +
            (args.AttestedCredentialData?.Length ?? 0) +
            (args.Extensions?.Length ?? 0);
        var result = new byte[length];
        var offset = 0;

        args.RpIdHash.Span.CopyTo(result.AsSpan(offset, RpIdHashLength));
        offset += RpIdHashLength;

        result[offset] = (byte)args.Flags;
        offset += AuthenticatorDataFlagsLength;

        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(offset, SignCountLength), args.SignCount);
        offset += SignCountLength;

        if (args.AttestedCredentialData is { } attestedCredentialData)
        {
            attestedCredentialData.Span.CopyTo(result.AsSpan(offset));
            offset += attestedCredentialData.Length;
        }

        if (args.Extensions is { } extensions)
        {
            extensions.Span.CopyTo(result.AsSpan(offset));
            offset += extensions.Length;
        }

        if (offset != result.Length)
        {
            throw new InvalidOperationException($"Expected authenticator data length '{length}', but got '{offset}'.");
        }

        return result;
    }

    public static ReadOnlyMemory<byte> MakeAttestationObject(in AttestationObjectArgs args)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(args.CborMapLength);
        if (args.Format is { } format)
        {
            writer.WriteTextString("fmt");
            writer.WriteTextString(format);
        }
        if (args.AttestationStatement is { } attestationStatement)
        {
            writer.WriteTextString("attStmt");
            writer.WriteEncodedValue(attestationStatement.Span);
        }
        if (args.AuthenticatorData is { } authenticatorData)
        {
            writer.WriteTextString("authData");
            writer.WriteByteString(authenticatorData.Span);
        }
        writer.WriteEndMap();
        return writer.Encode();
    }
}
