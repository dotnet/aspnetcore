// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class DefaultAntiforgeryTokenSerializer : IAntiforgeryTokenSerializer
{
    private const string Purpose = "Microsoft.AspNetCore.Antiforgery.AntiforgeryToken.v1";
    private const byte TokenVersion = 0x01;

    private readonly IDataProtector _cryptoSystem;
    private readonly ObjectPool<AntiforgerySerializationContext> _pool;

    public DefaultAntiforgeryTokenSerializer(
        IDataProtectionProvider provider,
        ObjectPool<AntiforgerySerializationContext> pool)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(pool);

        _cryptoSystem = provider.CreateProtector(Purpose);
        _pool = pool;
    }

    public AntiforgeryToken Deserialize(string serializedToken)
    {
        var serializationContext = _pool.Get();

        byte[]? rented = null;
        Exception? innerException = null;
        try
        {
            var tokenLength = serializedToken.Length;
            var charsRequired = WebEncoders.GetArraySizeRequiredToDecode(tokenLength);

            var chars = charsRequired < 128 ? stackalloc byte[128] : (rented = ArrayPool<byte>.Shared.Rent(charsRequired));
            chars = chars[..charsRequired];

            var decodedResult = WebEncoders.TryBase64UrlDecode(
                serializedToken,
                offset: 0,
                count: tokenLength,
                destination: chars,
                out var bytesWritten);
            Debug.Assert(decodedResult is true);

            // when DataProtection obtains Span<>'ish APIs,
            // we will just pass in chars directly. For now we need to allocate.
            var tokenBytes = chars.Slice(0, bytesWritten).ToArray();
            var unprotectedBytes = _cryptoSystem.Unprotect(tokenBytes);

            var token = Deserialize(unprotectedBytes);
            if (token != null)
            {
                return token;
            }
        }
        catch (Exception ex)
        {
            // swallow all exceptions - homogenize error if something went wrong
            innerException = ex;
        }
        finally
        {
            _pool.Return(serializationContext);

            if (rented is not null)
            {
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        // if we reached this point, something went wrong deserializing
        throw new AntiforgeryValidationException(Resources.AntiforgeryToken_DeserializationFailed, innerException);
    }

    /* The serialized format of the anti-XSRF token is as follows:
     * Version: 1 byte integer
     * SecurityToken: 16 byte binary blob
     * IsCookieToken: 1 byte Boolean
     * [if IsCookieToken != true]
     *   +- IsClaimsBased: 1 byte Boolean
     *   |  [if IsClaimsBased = true]
     *   |    `- ClaimUid: 32 byte binary blob
     *   |  [if IsClaimsBased = false]
     *   |    `- Username: UTF-8 string with 7-bit integer length prefix
     *   `- AdditionalData: UTF-8 string with 7-bit integer length prefix
     */
    private static AntiforgeryToken? Deserialize(ReadOnlySpan<byte> unprotectedBytes)
    {
        // pointer to current byte at unprotectedBytes
        var pointer = 0;

        // we can only consume tokens of the same serialized version that we generate
        var embeddedVersion = unprotectedBytes[pointer++];
        if (embeddedVersion != TokenVersion)
        {
            return null;
        }

        var deserializedToken = new AntiforgeryToken();

        var securityTokenBytesLength = AntiforgeryToken.SecurityTokenBitLength / 8;
        var securityTokenBytes = unprotectedBytes.Slice(pointer, securityTokenBytesLength).ToArray();
        pointer += securityTokenBytesLength;
        deserializedToken.SecurityToken = new BinaryBlob(AntiforgeryToken.SecurityTokenBitLength, securityTokenBytes);

        deserializedToken.IsCookieToken = unprotectedBytes.BinaryReadBoolean(ref pointer);
        if (!deserializedToken.IsCookieToken)
        {
            var isClaimsBased = unprotectedBytes.BinaryReadBoolean(ref pointer);
            if (isClaimsBased)
            {
                var claimUidBytesLength = AntiforgeryToken.ClaimUidBitLength / 8;
                var claimUidBytes = unprotectedBytes.Slice(pointer, claimUidBytesLength).ToArray();
                pointer += claimUidBytesLength;
                deserializedToken.ClaimUid = new BinaryBlob(AntiforgeryToken.ClaimUidBitLength, claimUidBytes);
            }
            else
            {
                deserializedToken.Username = unprotectedBytes.Slice(pointer).BinaryReadString(out var usernameBytesRead);
                pointer += usernameBytesRead;
            }

            deserializedToken.AdditionalData = unprotectedBytes.Slice(pointer).BinaryReadString(out var addDataBytesRead);
            pointer += addDataBytesRead;

        }

        // if there's still unconsumed data in the stream, fail
        if (pointer < unprotectedBytes.Length && unprotectedBytes[pointer] != 0x00)
        {
            return null;
        }

        // success
        return deserializedToken;
    }

    public string Serialize(AntiforgeryToken token)
    {
        ArgumentNullException.ThrowIfNull(token);

        var serializationContext = _pool.Get();

        try
        {
            var writer = serializationContext.Writer;
            writer.Write(TokenVersion);
            writer.Write(token.SecurityToken!.GetData());
            writer.Write(token.IsCookieToken);

            if (!token.IsCookieToken)
            {
                if (token.ClaimUid != null)
                {
                    writer.Write(true /* isClaimsBased */);
                    writer.Write(token.ClaimUid.GetData());
                }
                else
                {
                    writer.Write(false /* isClaimsBased */);
                    writer.Write(token.Username!);
                }

                writer.Write(token.AdditionalData);
            }

            writer.Flush();
            var stream = serializationContext.Stream;
            var bytes = _cryptoSystem.Protect(stream.ToArray());

            var count = bytes.Length;
            var charsRequired = WebEncoders.GetArraySizeRequiredToEncode(count);
            var chars = serializationContext.GetChars(charsRequired);
            var outputLength = WebEncoders.Base64UrlEncode(
                bytes,
                offset: 0,
                output: chars,
                outputOffset: 0,
                count: count);

            return new string(chars, startIndex: 0, length: outputLength);
        }
        finally
        {
            _pool.Return(serializationContext);
        }
    }
}
