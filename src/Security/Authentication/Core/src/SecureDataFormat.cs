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
            return ProtectWithSpans(spanProtector, userData);
        }

        var protectedData = protector.Protect(userData);
        return Base64UrlTextEncoder.Encode(protectedData);
    }

    private static string ProtectWithSpans(ISpanDataProtector protector, byte[] userData)
    {
        // The protected payload is the plaintext plus a small fixed overhead (header, IV, padding and MAC).
        var writer = new PooledByteBufferWriter(userData.Length + 256);
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
                ? UnprotectWithSpans(spanProtector, protectedText)
                : UnprotectWithArrays(protector, protectedText);
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

    private static byte[]? UnprotectWithArrays(IDataProtector protector, string protectedText)
    {
        var protectedData = Base64UrlTextEncoder.Decode(protectedText);
        if (protectedData == null)
        {
            return null;
        }

        return protector.Unprotect(protectedData);
    }

    private static byte[]? UnprotectWithSpans(ISpanDataProtector protector, string protectedText)
    {
        var protectedBuffer = ArrayPool<byte>.Shared.Rent(Base64Url.GetMaxDecodedLength(protectedText.Length));
        try
        {
            if (Base64Url.DecodeFromChars(protectedText, protectedBuffer, out _, out var protectedLength) != OperationStatus.Done)
            {
                // Base64UrlTextEncoder.Decode also accepts standard base64, so let it handle anything that isn't strict base64url.
                return UnprotectWithArrays(protector, protectedText);
            }

            // The plaintext is never larger than the protected payload.
            var writer = new PooledByteBufferWriter(protectedLength);
            try
            {
                protector.Unprotect(protectedBuffer.AsSpan(0, protectedLength), ref writer);
                return writer.WrittenSpan.ToArray();
            }
            finally
            {
                writer.Dispose();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(protectedBuffer);
        }
    }

    // A pooled IBufferWriter that supports both GetSpan and GetMemory, so any ISpanDataProtector implementation can write to it.
    // The buffer only lives for the duration of a single Protect/Unprotect call and is cleared when returned to the pool.
    private struct PooledByteBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer;
        private int _written;

        public PooledByteBufferWriter(int initialCapacity)
        {
            _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        }

        public readonly ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _written);

        public void Advance(int count)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, _buffer.Length - _written);
            _written += count;
        }

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_written);
        }

        public void Dispose()
        {
            var buffer = _buffer;
            _buffer = [];
            _written = 0;
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }

        private void EnsureCapacity(int sizeHint)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
            if (sizeHint == 0)
            {
                sizeHint = 1;
            }

            if (sizeHint > _buffer.Length - _written)
            {
                var newBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(checked(_written + sizeHint), checked(_buffer.Length * 2)));
                _buffer.AsSpan(0, _written).CopyTo(newBuffer);
                ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
                _buffer = newBuffer;
            }
        }
    }
}
