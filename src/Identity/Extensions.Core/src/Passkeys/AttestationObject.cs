// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Cbor;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents an authenticator attestation object, which contains the attestation statement and authenticator data.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#attestation-object"/>.
/// </remarks>
internal sealed class AttestationObject(string fmt, ReadOnlyMemory<byte> attStmt, ReadOnlyMemory<byte> authData)
{
    public string Fmt => fmt;

    public ReadOnlyMemory<byte> AttStmt => attStmt;

    public ReadOnlyMemory<byte> AuthData => authData;

    public static bool TryParse(ReadOnlyMemory<byte> data, [NotNullWhen(true)] out AttestationObject? result)
    {
        var reader = new CborReader(data);
        _ = reader.ReadStartMap();

        string? fmt = null;
        ReadOnlyMemory<byte>? attStmt = default;
        ReadOnlyMemory<byte>? authData = default;

        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "fmt":
                    fmt = reader.ReadTextString();
                    break;
                case "attStmt":
                    attStmt = reader.ReadEncodedValue();
                    break;
                case "authData":
                    authData = reader.ReadByteString();
                    break;
                default:
                    // Unknown key - skip.
                    reader.SkipValue();
                    break;
            }
        }

        if (fmt is null || !attStmt.HasValue || !authData.HasValue)
        {
            result = null;
            return false;
        }

        result = new(fmt, attStmt.Value, authData.Value);
        return true;
    }
}
