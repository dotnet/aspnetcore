// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication
{
    // This MUST be kept in sync with Microsoft.Owin.Security.Interop.AspNetTicketSerializer
    public class TicketSerializer : IDataSerializer<AuthenticationTicket>
    {
        private const string DefaultStringPlaceholder = "\0";
        private const int FormatVersion = 5;

        public static TicketSerializer Default { get; } = new TicketSerializer();

        public virtual byte[] Serialize(AuthenticationTicket ticket)
        {
            using (var memory = new MemoryStream())
            {
                using (var writer = new BinaryWriter(memory))
                {
                    Write(writer, ticket);
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

        public virtual void Write(BinaryWriter writer, AuthenticationTicket ticket)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (ticket == null)
            {
                throw new ArgumentNullException(nameof(ticket));
            }

            writer.Write(FormatVersion);
            writer.Write(ticket.AuthenticationScheme);

            // Write the number of identities contained in the principal.
            var principal = ticket.Principal;
            writer.Write(principal.Identities.Count());

            foreach (var identity in principal.Identities)
            {
                WriteIdentity(writer, identity);
            }

            PropertiesSerializer.Default.Write(writer, ticket.Properties);
        }

        protected virtual void WriteIdentity(BinaryWriter writer, ClaimsIdentity identity)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            var authenticationType = identity.AuthenticationType ?? string.Empty;

            writer.Write(authenticationType);
            WriteWithDefault(writer, identity.NameClaimType, ClaimsIdentity.DefaultNameClaimType);
            WriteWithDefault(writer, identity.RoleClaimType, ClaimsIdentity.DefaultRoleClaimType);

            // Write the number of claims contained in the identity.
            writer.Write(identity.Claims.Count());

            foreach (var claim in identity.Claims)
            {
                WriteClaim(writer, claim);
            }

            var bootstrap = identity.BootstrapContext as string;
            if (!string.IsNullOrEmpty(bootstrap))
            {
                writer.Write(true);
                writer.Write(bootstrap);
            }
            else
            {
                writer.Write(false);
            }

            if (identity.Actor != null)
            {
                writer.Write(true);
                WriteIdentity(writer, identity.Actor);
            }
            else
            {
                writer.Write(false);
            }
        }

        protected virtual void WriteClaim(BinaryWriter writer, Claim claim)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            WriteWithDefault(writer, claim.Type, claim.Subject?.NameClaimType ?? ClaimsIdentity.DefaultNameClaimType);
            writer.Write(claim.Value);
            WriteWithDefault(writer, claim.ValueType, ClaimValueTypes.String);
            WriteWithDefault(writer, claim.Issuer, ClaimsIdentity.DefaultIssuer);
            WriteWithDefault(writer, claim.OriginalIssuer, claim.Issuer);

            // Write the number of properties contained in the claim.
            writer.Write(claim.Properties.Count);

            foreach (var property in claim.Properties)
            {
                writer.Write(property.Key ?? string.Empty);
                writer.Write(property.Value ?? string.Empty);
            }
        }

        public virtual AuthenticationTicket Read(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (reader.ReadInt32() != FormatVersion)
            {
                return null;
            }

            var scheme = reader.ReadString();

            // Read the number of identities stored
            // in the serialized payload.
            var count = reader.ReadInt32();
            if (count < 0)
            {
                return null;
            }

            var identities = new ClaimsIdentity[count];
            for (var index = 0; index != count; ++index)
            {
                identities[index] = ReadIdentity(reader);
            }

            var properties = PropertiesSerializer.Default.Read(reader);

            return new AuthenticationTicket(new ClaimsPrincipal(identities), properties, scheme);
        }

        protected virtual ClaimsIdentity ReadIdentity(BinaryReader reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            var authenticationType = reader.ReadString();
            var nameClaimType = ReadWithDefault(reader, ClaimsIdentity.DefaultNameClaimType);
            var roleClaimType = ReadWithDefault(reader, ClaimsIdentity.DefaultRoleClaimType);

            // Read the number of claims contained
            // in the serialized identity.
            var count = reader.ReadInt32();

            var identity = new ClaimsIdentity(authenticationType, nameClaimType, roleClaimType);

            for (int index = 0; index != count; ++index)
            {
                var claim = ReadClaim(reader, identity);

                identity.AddClaim(claim);
            }

            // Determine whether the identity
            // has a bootstrap context attached.
            if (reader.ReadBoolean())
            {
                identity.BootstrapContext = reader.ReadString();
            }

            // Determine whether the identity
            // has an actor identity attached.
            if (reader.ReadBoolean())
            {
                identity.Actor = ReadIdentity(reader);
            }

            return identity;
        }

        protected virtual Claim ReadClaim(BinaryReader reader, ClaimsIdentity identity)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }

            var type = ReadWithDefault(reader, identity.NameClaimType);
            var value = reader.ReadString();
            var valueType = ReadWithDefault(reader, ClaimValueTypes.String);
            var issuer = ReadWithDefault(reader, ClaimsIdentity.DefaultIssuer);
            var originalIssuer = ReadWithDefault(reader, issuer);

            var claim = new Claim(type, value, valueType, issuer, originalIssuer, identity);

            // Read the number of properties stored in the claim.
            var count = reader.ReadInt32();

            for (var index = 0; index != count; ++index)
            {
                var key = reader.ReadString();
                var propertyValue = reader.ReadString();

                claim.Properties.Add(key, propertyValue);
            }

            return claim;
        }

        private static void WriteWithDefault(BinaryWriter writer, string value, string defaultValue)
        {
            if (string.Equals(value, defaultValue, StringComparison.Ordinal))
            {
                writer.Write(DefaultStringPlaceholder);
            }
            else
            {
                writer.Write(value);
            }
        }

        private static string ReadWithDefault(BinaryReader reader, string defaultValue)
        {
            var value = reader.ReadString();
            if (string.Equals(value, DefaultStringPlaceholder, StringComparison.Ordinal))
            {
                return defaultValue;
            }
            return value;
        }
    }
}
