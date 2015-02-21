// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.AspNet.WebUtilities;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc
{
    internal sealed class AntiForgeryTokenSerializer : IAntiForgeryTokenSerializer
    {
        private readonly IDataProtector _cryptoSystem;
        private const byte TokenVersion = 0x01;

        internal AntiForgeryTokenSerializer([NotNull] IDataProtector cryptoSystem)
        {
            _cryptoSystem = cryptoSystem;
        }

        public AntiForgeryToken Deserialize(string serializedToken)
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
            throw new InvalidOperationException(Resources.AntiForgeryToken_DeserializationFailed, innerException);
        }

        /* The serialized format of the anti-XSRF token is as follows:
         * Version: 1 byte integer
         * SecurityToken: 16 byte binary blob
         * IsSessionToken: 1 byte Boolean
         * [if IsSessionToken != true]
         *   +- IsClaimsBased: 1 byte Boolean
         *   |  [if IsClaimsBased = true]
         *   |    `- ClaimUid: 32 byte binary blob
         *   |  [if IsClaimsBased = false]
         *   |    `- Username: UTF-8 string with 7-bit integer length prefix
         *   `- AdditionalData: UTF-8 string with 7-bit integer length prefix
         */
        private static AntiForgeryToken DeserializeImpl(BinaryReader reader)
        {
            // we can only consume tokens of the same serialized version that we generate
            var embeddedVersion = reader.ReadByte();
            if (embeddedVersion != TokenVersion)
            {
                return null;
            }

            var deserializedToken = new AntiForgeryToken();
            var securityTokenBytes = reader.ReadBytes(AntiForgeryToken.SecurityTokenBitLength / 8);
            deserializedToken.SecurityToken =
                new BinaryBlob(AntiForgeryToken.SecurityTokenBitLength, securityTokenBytes);
            deserializedToken.IsSessionToken = reader.ReadBoolean();

            if (!deserializedToken.IsSessionToken)
            {
                var isClaimsBased = reader.ReadBoolean();
                if (isClaimsBased)
                {
                    var claimUidBytes = reader.ReadBytes(AntiForgeryToken.ClaimUidBitLength / 8);
                    deserializedToken.ClaimUid = new BinaryBlob(AntiForgeryToken.ClaimUidBitLength, claimUidBytes);
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

        public string Serialize([NotNull] AntiForgeryToken token)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(TokenVersion);
                    writer.Write(token.SecurityToken.GetData());
                    writer.Write(token.IsSessionToken);

                    if (!token.IsSessionToken)
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