// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Cbor;

namespace Microsoft.AspNetCore.Identity;

/// <summary>
/// Represents an authenticator attestation object, which contains the attestation statement and authenticator data.
/// </summary>
/// <remarks>
/// See <see href="https://www.w3.org/TR/webauthn-3/#attestation-object"/>.
/// </remarks>
internal sealed class AttestationObject
{
    /// <summary>
    /// Gets or sets the attestation statement format.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#attestation-statement-format"/>.
    /// </remarks>
    public required string Format { get; init; }

    /// <summary>
    /// Gets or sets the attestation statement.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#attestation-statement"/>.
    /// </remarks>
    public required ReadOnlyMemory<byte> AttestationStatement { get; init; }

    /// <summary>
    /// Gets or sets the authenticator data.
    /// </summary>
    /// <remarks>
    /// See <see href="https://www.w3.org/TR/webauthn-3/#authenticator-data"/>.
    /// </remarks>
    public required ReadOnlyMemory<byte> AuthenticatorData { get; init; }

    public static AttestationObject Parse(ReadOnlyMemory<byte> data)
    {
        try
        {
            return ParseCore(data);
        }
        catch (PasskeyException)
        {
            throw;
        }
        catch (CborContentException ex)
        {
            throw PasskeyException.InvalidAttestationObjectFormat(ex);
        }
        catch (InvalidOperationException ex)
        {
            throw PasskeyException.InvalidAttestationObjectFormat(ex);
        }
        catch (Exception ex)
        {
            throw PasskeyException.InvalidAttestationObject(ex);
        }
    }

    private static AttestationObject ParseCore(ReadOnlyMemory<byte> data)
    {
        var reader = new CborReader(data);
        _ = reader.ReadStartMap();

        string? format = null;
        ReadOnlyMemory<byte>? attestationStatement = default;
        ReadOnlyMemory<byte>? authenticatorData = default;

        while (reader.PeekState() != CborReaderState.EndMap)
        {
            var key = reader.ReadTextString();
            switch (key)
            {
                case "fmt":
                    format = reader.ReadTextString();
                    break;
                case "attStmt":
                    attestationStatement = reader.ReadEncodedValue();
                    break;
                case "authData":
                    authenticatorData = reader.ReadByteString();
                    break;
                default:
                    // Unknown key - skip.
                    reader.SkipValue();
                    break;
            }
        }

        if (format is null)
        {
            throw PasskeyException.MissingAttestationStatementFormat();
        }

        if (!attestationStatement.HasValue)
        {
            throw PasskeyException.MissingAttestationStatement();
        }

        if (!authenticatorData.HasValue)
        {
            throw PasskeyException.MissingAuthenticatorData();
        }

        return new()
        {
            Format = format,
            AttestationStatement = attestationStatement.Value,
            AuthenticatorData = authenticatorData.Value
        };
    }
}
