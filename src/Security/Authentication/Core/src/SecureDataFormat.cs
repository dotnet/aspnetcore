// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// An implementation for <see cref="ISecureDataFormat{TData}"/>.
/// </summary>
/// <typeparam name="TData"></typeparam>
public class SecureDataFormat<TData> : ISecureDataFormat<TData>
{
    // Size of the stack buffer used for payloads small enough to avoid renting from the pool.
    private const int StackBufferSize = 256;

    // Upper bound for the built-in encryptors' overhead: magic header and key id (20), IV (16), padding (16) and HMACSHA512 (64).
    private const int ProtectOverhead = 128;

    private readonly IDataSerializer<TData> _serializer;
    private readonly IDataProtector _protector;

    /// <summary>
    /// Initializes a new instance of <see cref="SecureDataFormat{TData}"/>.
    /// </summary>
    /// <param name="serializer">The <see cref="IDataSerializer{TModel}"/>.</param>
    /// <param name="protector">The <see cref="IDataProtector"/>.</param>
    public SecureDataFormat(IDataSerializer<TData> serializer, IDataProtector protector)
    {
        _serializer = serializer;
        _protector = protector;
    }

    /// <inheritdoc />
    public string Protect(TData data)
    {
        return Protect(data, purpose: null);
    }

    /// <inheritdoc />
    public string Protect(TData data, string? purpose)
    {
        var userData = _serializer.Serialize(data);

        var protector = _protector;
        if (!string.IsNullOrEmpty(purpose))
        {
            protector = protector.CreateProtector(purpose);
        }

        if (protector is ISpanDataProtector spanProtector)
        {
            try
            {
                return SpanProtect(spanProtector, userData);
            }
            catch (UnreachableException)
            {
                // RefPooledArrayBufferWriter does not support GetMemory, so use the array API for protectors that need it.
            }
        }

        var protectedData = protector.Protect(userData);
        return Base64UrlTextEncoder.Encode(protectedData);
    }

    private static string SpanProtect(ISpanDataProtector protector, byte[] userData)
    {
        // The protected payload is the plaintext plus a small fixed overhead (header, IV, padding and MAC).
        var estimatedLength = userData.Length + ProtectOverhead;
        byte[]? rentedBuffer = null;
        var initialBuffer = estimatedLength <= StackBufferSize
            ? stackalloc byte[StackBufferSize]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(estimatedLength));

        var writer = new RefPooledArrayBufferWriter<byte>(initialBuffer);
        try
        {
            protector.Protect(userData, ref writer);
            return WebEncoders.Base64UrlEncode(writer.WrittenSpan);
        }
        finally
        {
            writer.Dispose();
            ReturnBuffer(initialBuffer, rentedBuffer);
        }
    }

    /// <inheritdoc />
    public TData? Unprotect(string? protectedText)
    {
        return Unprotect(protectedText, purpose: null);
    }

    /// <inheritdoc />
    public TData? Unprotect(string? protectedText, string? purpose)
    {
        try
        {
            if (protectedText == null)
            {
                return default(TData);
            }

            var protector = _protector;
            if (!string.IsNullOrEmpty(purpose))
            {
                protector = protector.CreateProtector(purpose);
            }

            var userData = protector is ISpanDataProtector spanProtector
                ? SpanUnprotect(spanProtector, protectedText)
                : Unprotect(protector, protectedText);
            if (userData == null)
            {
                return default(TData);
            }

            return _serializer.Deserialize(userData);
        }
        catch
        {
            // TODO trace exception, but do not leak other information
            return default(TData);
        }
    }

    private static byte[]? Unprotect(IDataProtector protector, string protectedText)
    {
        var protectedData = Base64UrlTextEncoder.Decode(protectedText);
        if (protectedData is null)
        {
            return null;
        }

        return protector.Unprotect(protectedData);
    }

    private static byte[]? SpanUnprotect(ISpanDataProtector protector, string protectedText)
    {
        var protectedBuffer = ArrayPool<byte>.Shared.Rent(Base64Url.GetMaxDecodedLength(protectedText.Length));
        try
        {
            if (Base64Url.DecodeFromChars(protectedText, protectedBuffer, out _, out var protectedLength) != OperationStatus.Done)
            {
                // Base64UrlTextEncoder.Decode also accepts standard base64, so let it handle anything that isn't strict base64url.
                return Unprotect(protector, protectedText);
            }

            // The plaintext is never larger than the protected payload.
            byte[]? rentedBuffer = null;
            var initialBuffer = protectedLength <= StackBufferSize
                ? stackalloc byte[StackBufferSize]
                : (rentedBuffer = ArrayPool<byte>.Shared.Rent(protectedLength));

            var writer = new RefPooledArrayBufferWriter<byte>(initialBuffer);
            try
            {
                protector.Unprotect(protectedBuffer.AsSpan(0, protectedLength), ref writer);
                return writer.WrittenSpan.ToArray();
            }
            catch (UnreachableException)
            {
                // RefPooledArrayBufferWriter does not support GetMemory, so use the array API for protectors that need it.
                return protector.Unprotect(protectedBuffer.AsSpan(0, protectedLength).ToArray());
            }
            finally
            {
                writer.Dispose();
                ReturnBuffer(initialBuffer, rentedBuffer);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(protectedBuffer);
        }
    }

    // The initial buffer can hold plaintext, so it is always cleared before it goes out of scope or back to the pool.
    private static void ReturnBuffer(Span<byte> initialBuffer, byte[]? rentedBuffer)
    {
        if (rentedBuffer is null)
        {
            initialBuffer.Clear();
        }
        else
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer, clearArray: true);
        }
    }
}
