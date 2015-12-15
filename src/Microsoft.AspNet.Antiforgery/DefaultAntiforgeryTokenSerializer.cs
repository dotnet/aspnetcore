// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.DataProtection;
using Microsoft.AspNet.WebUtilities;

namespace Microsoft.AspNet.Antiforgery
{
    public class DefaultAntiforgeryTokenSerializer : IAntiforgeryTokenSerializer
    {
        private static readonly string Purpose = "Microsoft.AspNet.Antiforgery.AntiforgeryToken.v1";

        private readonly IDataProtector _cryptoSystem;
        private const byte TokenVersion = 0x01;

        public DefaultAntiforgeryTokenSerializer(IDataProtectionProvider provider)
        {
            if (provider == null)
            {
                throw new ArgumentNullException(nameof(provider));
            }

            _cryptoSystem = provider.CreateProtector(Purpose);
        }

        public AntiforgeryToken Deserialize(string serializedToken)
        {
            Exception innerException = null;
            try
            {
                var tokenBytes = WebEncoders.Base64UrlDecode(serializedToken);
                using (var stream = new MemoryStream(_cryptoSystem.Unprotect(tokenBytes)))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        var token = DeserializeImpl(reader);
                        if (token != null)
                        {
                            return token;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // swallow all exceptions - homogenize error if something went wrong
                innerException = ex;
            }

            // if we reached this point, something went wrong deserializing
            throw new InvalidOperationException(Resources.AntiforgeryToken_DeserializationFailed, innerException);
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
        private static AntiforgeryToken DeserializeImpl(BinaryReader reader)
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
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(TokenVersion);
                    writer.Write(token.SecurityToken.GetData());
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
                            writer.Write(token.Username);
                        }

                        writer.Write(token.AdditionalData);
                    }

                    writer.Flush();
                    return WebEncoders.Base64UrlEncode(_cryptoSystem.Protect(stream.ToArray()));
                }
            }
        }
    }
}