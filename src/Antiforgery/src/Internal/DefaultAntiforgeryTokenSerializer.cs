// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        Exception? innerException = null;
        try
        {
            var count = serializedToken.Length;
            var charsRequired = WebEncoders.GetArraySizeRequiredToDecode(count);
            var chars = serializationContext.GetChars(charsRequired);
            var tokenBytes = WebEncoders.Base64UrlDecode(
                serializedToken,
                offset: 0,
                buffer: chars,
                bufferOffset: 0,
                count: count);

            var unprotectedBytes = _cryptoSystem.Unprotect(tokenBytes);
            var stream = serializationContext.Stream;
            stream.Write(unprotectedBytes, offset: 0, count: unprotectedBytes.Length);
            stream.Position = 0L;

            var reader = serializationContext.Reader;
            var token = Deserialize(reader);
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
    private static AntiforgeryToken? Deserialize(BinaryReader reader)
    {
        // we can only consume tokens of the same serialized version that we generate
        var embeddedVersion = reader.ReadByte();
        if (embeddedVersion != TokenVersion)
        {
            return null;
        }

        var deserializedToken = new AntiforgeryToken();
        var securityTokenBytes = reader.ReadBytes(AntiforgeryToken.SecurityTokenBitLength / 8);
        deserializedToken.SecurityToken =
            new BinaryBlob(AntiforgeryToken.SecurityTokenBitLength, securityTokenBytes);
        deserializedToken.IsCookieToken = reader.ReadBoolean();

        if (!deserializedToken.IsCookieToken)
        {
            var isClaimsBased = reader.ReadBoolean();
            if (isClaimsBased)
            {
                var claimUidBytes = reader.ReadBytes(AntiforgeryToken.ClaimUidBitLength / 8);
                deserializedToken.ClaimUid = new BinaryBlob(AntiforgeryToken.ClaimUidBitLength, claimUidBytes);
            }
            else
            {
                deserializedToken.Username = reader.ReadString();
            }

            deserializedToken.AdditionalData = reader.ReadString();
        }

        // if there's still unconsumed data in the stream, fail
        if (reader.BaseStream.ReadByte() != -1)
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
