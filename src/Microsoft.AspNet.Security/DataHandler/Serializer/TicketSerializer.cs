// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.AspNet.Security.DataHandler.Serializer
{
    public class TicketSerializer : IDataSerializer<AuthenticationTicket>
    {
        private const int FormatVersion = 2;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Dispose is idempotent")]
        public virtual byte[] Serialize(AuthenticationTicket model)
        {
            using (var memory = new MemoryStream())
            {
                using (var compression = new GZipStream(memory, CompressionLevel.Optimal))
                {
                    using (var writer = new BinaryWriter(compression))
                    {
                        Write(writer, model);
                    }
                }
                return memory.ToArray();
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "Dispose is idempotent")]
        public virtual AuthenticationTicket Deserialize(byte[] data)
        {
            using (var memory = new MemoryStream(data))
            {
                using (var compression = new GZipStream(memory, CompressionMode.Decompress))
                {
                    using (var reader = new BinaryReader(compression))
                    {
                        return Read(reader);
                    }
                }
            }
        }

        public static void Write([NotNull] BinaryWriter writer, [NotNull] AuthenticationTicket model)
        {
            writer.Write(FormatVersion);
            ClaimsIdentity identity = model.Identity;
            writer.Write(identity.AuthenticationType);
            WriteWithDefault(writer, identity.NameClaimType, DefaultValues.NameClaimType);
            WriteWithDefault(writer, identity.RoleClaimType, DefaultValues.RoleClaimType);
            writer.Write(identity.Claims.Count());
            foreach (var claim in identity.Claims)
            {
                WriteWithDefault(writer, claim.Type, identity.NameClaimType);
                writer.Write(claim.Value);
                WriteWithDefault(writer, claim.ValueType, DefaultValues.StringValueType);
                WriteWithDefault(writer, claim.Issuer, DefaultValues.LocalAuthority);
                WriteWithDefault(writer, claim.OriginalIssuer, claim.Issuer);
            }
            PropertiesSerializer.Write(writer, model.Properties);
        }

        public static AuthenticationTicket Read([NotNull] BinaryReader reader)
        {
            if (reader.ReadInt32() != FormatVersion)
            {
                return null;
            }

            string authenticationType = reader.ReadString();
            string nameClaimType = ReadWithDefault(reader, DefaultValues.NameClaimType);
            string roleClaimType = ReadWithDefault(reader, DefaultValues.RoleClaimType);
            int count = reader.ReadInt32();
            var claims = new Claim[count];
            for (int index = 0; index != count; ++index)
            {
                string type = ReadWithDefault(reader, nameClaimType);
                string value = reader.ReadString();
                string valueType = ReadWithDefault(reader, DefaultValues.StringValueType);
                string issuer = ReadWithDefault(reader, DefaultValues.LocalAuthority);
                string originalIssuer = ReadWithDefault(reader, issuer);
                claims[index] = new Claim(type, value, valueType, issuer, originalIssuer);
            }
            var identity = new ClaimsIdentity(claims, authenticationType, nameClaimType, roleClaimType);
            var properties = PropertiesSerializer.Read(reader);
            return new AuthenticationTicket(identity, properties);
        }

        private static void WriteWithDefault(BinaryWriter writer, string value, string defaultValue)
        {
            if (string.Equals(value, defaultValue, StringComparison.Ordinal))
            {
                writer.Write(DefaultValues.DefaultStringPlaceholder);
            }
            else
            {
                writer.Write(value);
            }
        }

        private static string ReadWithDefault(BinaryReader reader, string defaultValue)
        {
            string value = reader.ReadString();
            if (string.Equals(value, DefaultValues.DefaultStringPlaceholder, StringComparison.Ordinal))
            {
                return defaultValue;
            }
            return value;
        }

        private static class DefaultValues
        {
            public const string DefaultStringPlaceholder = "\0";
            public const string NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
            public const string RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
            public const string LocalAuthority = "LOCAL AUTHORITY";
            public const string StringValueType = "http://www.w3.org/2001/XMLSchema#string";
        }
    }
}
