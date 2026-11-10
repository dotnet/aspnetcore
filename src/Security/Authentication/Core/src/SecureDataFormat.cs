// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// An implementation for <see cref="ISecureDataFormat{TData}"/>.
/// </summary>
/// <typeparam name="TData"></typeparam>
public class SecureDataFormat<TData> : ISecureDataFormat<TData>
{
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
            return SpanProtect(spanProtector, userData);
        }

        var protectedData = protector.Protect(userData);
        return Base64UrlTextEncoder.Encode(protectedData);
    }

    private static string SpanProtect(ISpanDataProtector protector, byte[] userData)
    {
        var writer = new RefPooledArrayBufferWriter<byte>(stackalloc byte[256]);
        try
        {
            protector.Protect(userData, ref writer);
            return WebEncoders.Base64UrlEncode(writer.WrittenSpan);
        }
        finally
        {
            writer.Dispose();
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

            Span<byte> stackBuffer = stackalloc byte[256];
            var writer = new RefPooledArrayBufferWriter<byte>(stackBuffer);
            try
            {
                protector.Unprotect(protectedBuffer.AsSpan(0, protectedLength), ref writer);
                return writer.WrittenSpan.ToArray();
            }
            finally
            {
                // Dispose clears any rented buffer; the stack buffer can also hold plaintext.
                writer.Dispose();
                stackBuffer.Clear();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(protectedBuffer);
        }
    }
}
