// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.IO;
using Microsoft.AspNet.Mvc.Core;
using Microsoft.AspNet.Security.DataProtection;
using System.Text;

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
                var tokenBytes = UrlTokenDecode(serializedToken);
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
            catch(Exception ex)
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
            deserializedToken.SecurityToken = new BinaryBlob(AntiForgeryToken.SecurityTokenBitLength, securityTokenBytes);
            deserializedToken.IsSessionToken = reader.ReadBoolean();

            if (!deserializedToken.IsSessionToken)
            {
                bool isClaimsBased = reader.ReadBoolean();
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
                    return UrlTokenEncode(_cryptoSystem.Protect(stream.ToArray()));
                }
            }
        }

        private string UrlTokenEncode(byte[] input)
        {
            var base64String = Convert.ToBase64String(input);
            if (string.IsNullOrEmpty(base64String))
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            for (int i = 0; i < base64String.Length; i++)
            {
                switch (base64String[i])
                {
                    case '+':
                        sb.Append('-');
                        break;
                    case '/':
                        sb.Append('_');
                        break;
                    case '=':
                        sb.Append('.');
                        break;
                    default:
                        sb.Append(base64String[i]);
                        break;
                }
            }

            return sb.ToString();
        }

        private byte[] UrlTokenDecode(string input)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                switch (input[i])
                {
                    case '-':
                        sb.Append('+');
                        break;
                    case '_':
                        sb.Append('/');
                        break;
                    case '.':
                        sb.Append('=');
                        break;
                    default:
                        sb.Append(input[i]);
                        break;
                }
            }

            return Convert.FromBase64String(sb.ToString());
        }
    }
}