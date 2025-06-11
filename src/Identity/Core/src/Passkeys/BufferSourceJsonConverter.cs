// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Identity;

internal sealed class BufferSourceJsonConverter : JsonConverter<BufferSource>
{
    private const int StackallocByteThreshold = 256;

    public override BufferSource? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.ValueIsEscaped)
        {
            // We currently don't handle escaped base64url values, as we don't expect
            // to encounter them when reading payloads produced by WebAuthn clients.
            // See: https://www.w3.org/TR/webauthn-3/#base64url-encoding
            throw new JsonException("Unexpected escaped value in base64url string.");
        }

        var span = reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan;
        if (!TryDecodeBase64Url(span, out var bytes))
        {
            throw new JsonException("Expected a valid base64url string.");
        }

        return BufferSource.FromBytes(bytes);
    }

    public override void Write(Utf8JsonWriter writer, BufferSource value, JsonSerializerOptions options)
    {
        var bytes = value.AsSpan();
        WriteBase64UrlStringValue(writer, bytes);
    }

    // Based on https://github.com/dotnet/runtime/blob/624737eb3796e1a760465912b27ac349965d8ba5/src/libraries/System.Text.Json/src/System/Text/Json/Reader/JsonReaderHelper.Unescaping.cs#L218
    private static bool TryDecodeBase64Url(ReadOnlySpan<byte> utf8Unescaped, [NotNullWhen(true)] out byte[]? bytes)
    {
        byte[]? pooledArray = null;

        Span<byte> byteSpan = utf8Unescaped.Length <= StackallocByteThreshold ?
            stackalloc byte[StackallocByteThreshold] :
            (pooledArray = ArrayPool<byte>.Shared.Rent(utf8Unescaped.Length));

        var status = Base64Url.DecodeFromUtf8(utf8Unescaped, byteSpan, out var bytesConsumed, out var bytesWritten);
        if (status != OperationStatus.Done)
        {
            bytes = null;

            if (pooledArray != null)
            {
                ArrayPool<byte>.Shared.Return(pooledArray);
            }

            return false;
        }
        Debug.Assert(bytesConsumed == utf8Unescaped.Length);

        bytes = byteSpan[..bytesWritten].ToArray();

        if (pooledArray != null)
        {
            ArrayPool<byte>.Shared.Return(pooledArray);
        }

        return true;
    }

    private static void WriteBase64UrlStringValue(Utf8JsonWriter writer, ReadOnlySpan<byte> bytes)
    {
        byte[]? pooledArray = null;

        var encodedLength = Base64Url.GetEncodedLength(bytes.Length);
        var byteSpan = encodedLength <= StackallocByteThreshold ?
            stackalloc byte[encodedLength] :
            (pooledArray = ArrayPool<byte>.Shared.Rent(encodedLength));

        var status = Base64Url.EncodeToUtf8(bytes, byteSpan, out var bytesConsumed, out var bytesWritten);
        Debug.Assert(status == OperationStatus.Done);
        Debug.Assert(bytesConsumed == bytes.Length);

        var base64UrlUtf8 = byteSpan[..bytesWritten];
        writer.WriteStringValue(base64UrlUtf8);

        if (pooledArray != null)
        {
            ArrayPool<byte>.Shared.Return(pooledArray);
        }
    }
}
