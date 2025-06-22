// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers.Binary;
using System.Buffers.Text;
using System.Formats.Cbor;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Microsoft.AspNetCore.Identity.Test;

public partial class DefaultPasskeyHandlerTest
{
    private static string ToJsonValue(string? value)
        => value is null ? "null" : $"\"{value}\"";

    private static string ToBase64UrlJsonValue(ReadOnlyMemory<byte>? bytes)
        => !bytes.HasValue ? "null" : $"\"{Base64Url.EncodeToString(bytes.Value.Span)}\"";

    private static string ToBase64UrlJsonValue(string? value)
        => value is null ? "null" : $"\"{Base64Url.EncodeToString(Encoding.UTF8.GetBytes(value))}\"";

    private static ReadOnlyMemory<byte> MakeAttestedCredentialData(in AttestedCredentialDataArgs args)
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

    private static ReadOnlyMemory<byte> MakeAuthenticatorData(in AuthenticatorDataArgs args)
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

    private static ReadOnlyMemory<byte> MakeAttestationObject(in AttestationObjectArgs args)
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

    private readonly struct AttestedCredentialDataArgs()
    {
        private static readonly ReadOnlyMemory<byte> _defaultAaguid = new byte[16];

        public ReadOnlyMemory<byte> Aaguid { get; init; } = _defaultAaguid;
        public required ReadOnlyMemory<byte> CredentialId { get; init; }
        public required ReadOnlyMemory<byte> CredentialPublicKey { get; init; }
    }

    private readonly struct AuthenticatorDataArgs()
    {
        public required AuthenticatorDataFlags Flags { get; init; }
        public required ReadOnlyMemory<byte> RpIdHash { get; init; }
        public ReadOnlyMemory<byte>? AttestedCredentialData { get; init; }
        public ReadOnlyMemory<byte>? Extensions { get; init; }
        public uint SignCount { get; init; } = 1;
    }

    private readonly struct AttestationObjectArgs()
    {
        private static readonly byte[] _defaultAttestationStatement = [0xA0]; // Empty CBOR map

        public int? CborMapLength { get; init; } = 3;
        public string? Format { get; init; } = "none";
        public ReadOnlyMemory<byte>? AttestationStatement { get; init; } = _defaultAttestationStatement;
        public required ReadOnlyMemory<byte>? AuthenticatorData { get; init; }
    }

    // Represents a test scenario for a passkey operation (attestation or assertion)
    private abstract class PasskeyTestBase<TResult>
    {
        private bool _hasStarted;

        public Task<TResult> RunAsync()
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("The test can only be run once.");
            }

            _hasStarted = true;
            return RunCoreAsync();
        }

        protected abstract Task<TResult> RunCoreAsync();
    }

    // While some test configuration can be set directly on scenario classes (AttestationTest and AssertionTest),
    // individual tests may need to modify values computed during execution (e.g., JSON payloads, hashes).
    // This helper enables trivial customization of test scenarios by allowing injection of custom logic to
    // transform runtime values.
    private class ComputedValue<TValue>
    {
        private bool _isComputed;
        private TValue? _computedValue;
        private Func<TValue, TValue?>? _transformFunc;

        public TValue GetValue()
        {
            if (!_isComputed)
            {
                throw new InvalidOperationException("Cannot get the value because it has not yet been computed.");
            }

            return _computedValue!;
        }

        public virtual TValue Compute(TValue initialValue)
        {
            if (_isComputed)
            {
                throw new InvalidOperationException("Cannot compute a value multiple times.");
            }

            if (_transformFunc is not null)
            {
                initialValue = _transformFunc(initialValue) ?? initialValue;
            }

            _isComputed = true;
            _computedValue = initialValue;
            return _computedValue;
        }

        public virtual void Transform(Func<TValue, TValue?> transform)
        {
            if (_transformFunc is not null)
            {
                throw new InvalidOperationException("Cannot transform a value multiple times.");
            }

            _transformFunc = transform;
        }
    }

    private sealed class ComputedJsonObject : ComputedValue<string>
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = true,
        };

        private JsonElement? _jsonElementValue;

        public JsonElement GetValueAsJsonElement()
        {
            if (_jsonElementValue is null)
            {
                var rawValue = GetValue() ?? throw new InvalidOperationException("Cannot get the value as a JSON element because it is null.");
                try
                {
                    _jsonElementValue = JsonSerializer.Deserialize<JsonElement>(rawValue, _jsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException("Cannot get the value as a JSON element because it is not valid JSON.", ex);
                }
            }

            return _jsonElementValue.Value;
        }

        public void TransformAsJsonObject(Action<JsonObject> transform)
        {
            Transform(value =>
            {
                try
                {
                    var jsonObject = JsonNode.Parse(value)?.AsObject()
                        ?? throw new InvalidOperationException("Could not transform the JSON value because it was unexpectedly null.");
                    transform(jsonObject);
                    return jsonObject.ToJsonString(_jsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException("Could not transform the value because it was not valid JSON.", ex);
                }
            });
        }
    }
}
