// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Shared;

namespace Microsoft.AspNetCore.Antiforgery;

internal sealed class DefaultAntiforgeryTokenSerializer : IAntiforgeryTokenSerializer
{
    private const string Purpose = "Microsoft.AspNetCore.Antiforgery.AntiforgeryToken.v1";
    private const byte TokenVersion = 0x01;

    private readonly IDataProtector _defaultCryptoSystem;
    private readonly ISpanDataProtector? _perfCryptoSystem;

    public DefaultAntiforgeryTokenSerializer(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);

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

            if (_perfCryptoSystem is not null)
            {
                var protectBuffer = new RefPooledArrayBufferWriter<byte>(stackalloc byte[255]);
                try
                {
                    _perfCryptoSystem!.Unprotect(tokenBytesDecoded, ref protectBuffer);
                    var token = Deserialize(protectBuffer.WrittenSpan);
                    if (token is not null)
                    {
                        return token;
                    }
                }
                finally
                {
                    protectBuffer.Dispose();
                }
            }
            else
            {
                var unprotectedBytes = _defaultCryptoSystem.Unprotect(tokenBytesDecoded.ToArray());
                var token = Deserialize(unprotectedBytes);
                if (token is not null)
                {
                    return token;
                }
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

        var securityTokenBytes = token.SecurityToken!.GetData();
        var claimUidBytes = token.ClaimUid?.GetData();

        var totalSize =
            1 // TokenVersion
            + securityTokenBytes.Length + // SecurityToken
            + 1; // IsCookieToken
        if (!token.IsCookieToken)
        {
            totalSize += 1; // isClaimsBased

            if (token.ClaimUid is not null)
            {
                totalSize += claimUidBytes!.Length;
            }
            else
            {
                var usernameByteCount = System.Text.Encoding.UTF8.GetByteCount(token.Username!);
                totalSize += usernameByteCount.Measure7BitEncodedUIntLength() + usernameByteCount;
            }

            var additionalDataByteCount = System.Text.Encoding.UTF8.GetByteCount(token.AdditionalData);
            totalSize += additionalDataByteCount.Measure7BitEncodedUIntLength() + additionalDataByteCount;
        }

        byte[]? tokenBytesRent = null;

        var rent = totalSize < 256
            ? stackalloc byte[255]
            : (tokenBytesRent = ArrayPool<byte>.Shared.Rent(totalSize));
        var tokenBytes = rent[..totalSize];

        try
        {
            var offset = 0;
            tokenBytes[offset++] = TokenVersion;
            securityTokenBytes.CopyTo(tokenBytes.Slice(offset, securityTokenBytes.Length));
            offset += securityTokenBytes.Length;
            tokenBytes[offset++] = token.IsCookieToken ? (byte)1 : (byte)0;

            if (!token.IsCookieToken)
            {
                if (token.ClaimUid != null)
                {
                    tokenBytes[offset++] = 1; // isClaimsBased
                    claimUidBytes!.CopyTo(tokenBytes.Slice(offset, claimUidBytes!.Length));
                    offset += claimUidBytes.Length;
                }
                else
                {
                    tokenBytes[offset++] = 0; // isClaimsBased
                    offset += tokenBytes[offset..].Write7BitEncodedString(token.Username!);
                }
                offset += tokenBytes[offset..].Write7BitEncodedString(token.AdditionalData);
            }

            if (_perfCryptoSystem is not null)
            {
                var protectBuffer = new RefPooledArrayBufferWriter<byte>(stackalloc byte[255]);
                try
                {
                    _perfCryptoSystem!.Protect(tokenBytes, ref protectBuffer);
                    return Base64Url.EncodeToString(protectBuffer.WrittenSpan);
                }
                finally
                {
                    protectBuffer.Dispose();
                }
            }
            else
            {
                var protectedBytes = _defaultCryptoSystem.Protect(tokenBytes.ToArray());
                return Base64Url.EncodeToString(protectedBytes);
            }
        }
        finally
        {
            if (tokenBytesRent is not null)
            {
                ArrayPool<byte>.Shared.Return(tokenBytesRent);
            }
        }
    }
}
