// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class DefaultAntiforgeryTokenSerializer : IAntiforgeryTokenSerializer
{
    private const string Purpose = "Microsoft.AspNetCore.Antiforgery.AntiforgeryToken.v1";
    private const byte TokenVersion = 0x01;

    private readonly IDataProtector _defaultCryptoSystem;
    private readonly ISpanDataProtector? _perfCryptoSystem;

    private readonly ObjectPool<AntiforgerySerializationContext> _pool;

    public DefaultAntiforgeryTokenSerializer(
        IDataProtectionProvider provider,
        ObjectPool<AntiforgerySerializationContext> pool)
    {
        ArgumentNullException.ThrowIfNull(provider);
        ArgumentNullException.ThrowIfNull(pool);

        _pool = pool;

        _defaultCryptoSystem = provider.CreateProtector(Purpose);
        _perfCryptoSystem = _defaultCryptoSystem as ISpanDataProtector;
    }

    public AntiforgeryToken Deserialize(string serializedToken)
    {
        byte[]? tokenBytesRent = null;
        Exception? innerException = null;
        try
        {
            var tokenDecodedSize = Base64Url.GetMaxDecodedLength(serializedToken.Length);

            var rent = tokenDecodedSize < 256
                ? stackalloc byte[255]
                : (tokenBytesRent = ArrayPool<byte>.Shared.Rent(tokenDecodedSize));
            var tokenBytes = rent[..tokenDecodedSize];

            var status = Base64Url.DecodeFromChars(serializedToken, tokenBytes, out int charsConsumed, out int bytesWritten);
            if (status is not OperationStatus.Done)
            {
                throw new FormatException("Failed to decode token as Base64 char sequence.");
            }

            var tokenBytesDecoded = tokenBytes.Slice(0, bytesWritten);
            var protectBuffer = new RefPooledArrayBufferWriter<byte>(stackalloc byte[255]);
            _perfCryptoSystem!.Unprotect(tokenBytesDecoded, ref protectBuffer);

            var token = Deserialize(protectBuffer.WrittenSpan);
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
            if (tokenBytesRent is not null)
            {
                ArrayPool<byte>.Shared.Return(tokenBytesRent);
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
    private static AntiforgeryToken? Deserialize(ReadOnlySpan<byte> tokenBytes)
    {
        var offset = 0;

        // we can only consume tokens of the same serialized version that we generate
        if (tokenBytes.Length < 1)
        {
            return null;
        }

        var embeddedVersion = tokenBytes[offset++];
        if (embeddedVersion != TokenVersion)
        {
            return null;
        }

        var deserializedToken = new AntiforgeryToken();

        // Read SecurityToken (16 bytes)
        const int securityTokenByteLength = AntiforgeryToken.SecurityTokenBitLength / 8;
        if (tokenBytes.Length < offset + securityTokenByteLength)
        {
            return null;
        }

        deserializedToken.SecurityToken = new BinaryBlob(
            AntiforgeryToken.SecurityTokenBitLength,
            tokenBytes.Slice(offset, securityTokenByteLength).ToArray());
        offset += securityTokenByteLength;

        // Read IsCookieToken (1 byte)
        if (tokenBytes.Length < offset + 1)
        {
            return null;
        }

        deserializedToken.IsCookieToken = tokenBytes[offset++] != 0;

        if (!deserializedToken.IsCookieToken)
        {
            // Read IsClaimsBased (1 byte)
            if (tokenBytes.Length < offset + 1)
            {
                return null;
            }

            var isClaimsBased = tokenBytes[offset++] != 0;
            if (isClaimsBased)
            {
                // Read ClaimUid (32 bytes)
                const int claimUidByteLength = AntiforgeryToken.ClaimUidBitLength / 8;
                if (tokenBytes.Length < offset + claimUidByteLength)
                {
                    return null;
                }

                deserializedToken.ClaimUid = new BinaryBlob(
                    AntiforgeryToken.ClaimUidBitLength,
                    tokenBytes.Slice(offset, claimUidByteLength).ToArray());
                offset += claimUidByteLength;
            }
            else
            {
                // Read Username (7-bit encoded length prefix + UTF-8 string)
                offset += tokenBytes.Slice(offset).Read7BitEncodedString(out var username);
                deserializedToken.Username = username;
            }

            offset += tokenBytes.Slice(offset).Read7BitEncodedString(out var additionalData);
            deserializedToken.AdditionalData = additionalData;
        }

        // if there's still unconsumed data in the span, fail
        if (offset != tokenBytes.Length)
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
            var bytes = _defaultCryptoSystem.Protect(stream.ToArray());

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
