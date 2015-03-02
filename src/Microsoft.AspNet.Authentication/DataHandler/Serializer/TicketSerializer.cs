// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.AspNet.Authentication.DataHandler.Serializer
{
    public class TicketSerializer : IDataSerializer<AuthenticationTicket>
    {
        private const int FormatVersion = 2;

        public virtual byte[] Serialize(AuthenticationTicket model)
        {
            using (var memory = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memory))
                {
                    Write(writer, model);
                }
                return memory.ToArray();
            }
        }

        public virtual AuthenticationTicket Deserialize(byte[] data)
        {
            using (var memory = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(memory))
                {
                    return Read(reader);
                }
            }
        }

        public static void Write([NotNull] BinaryWriter writer, [NotNull] AuthenticationTicket model)
        {
            writer.Write(FormatVersion);
            writer.Write(model.AuthenticationScheme);
            var principal = model.Principal;
            writer.Write(principal.Identities.Count());
            foreach (var identity in principal.Identities)
            {
                var authenticationType = string.IsNullOrWhiteSpace(identity.AuthenticationType) ? string.Empty : identity.AuthenticationType;
                writer.Write(authenticationType);
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
            }
            PropertiesSerializer.Write(writer, model.Properties);
        }

        public static AuthenticationTicket Read([NotNull] BinaryReader reader)
        {
            if (reader.ReadInt32() != FormatVersion)
            {
                return null;
            }
            string authenticationScheme = reader.ReadString();
            int identityCount = reader.ReadInt32();
            var identities = new ClaimsIdentity[identityCount];
            for (int i = 0; i != identityCount; ++i)
            {
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
                identities[i] = new ClaimsIdentity(claims, authenticationType, nameClaimType, roleClaimType);
            }
            var properties = PropertiesSerializer.Read(reader);
            return new AuthenticationTicket(new ClaimsPrincipal(identities), properties, authenticationScheme);
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
