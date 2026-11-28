// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Claims;

namespace Microsoft.AspNetCore.Authentication;

// This MUST be kept in sync with Microsoft.Owin.Security.Interop.AspNetTicketSerializer
/// <summary>
/// Serializes and deserializes <see cref="AuthenticationTicket"/> instances.
/// </summary>
public class TicketSerializer : IDataSerializer<AuthenticationTicket>
{
    internal const string DefaultStringPlaceholder = "\0";
    private const int FormatVersion = 5;

    /// <summary>
    /// Gets the default implementation for <see cref="TicketSerializer"/>.
    /// </summary>
    public static TicketSerializer Default { get; } = new TicketSerializer();

    /// <summary>
    /// Serializes the specified authentication ticket.
    /// </summary>
    /// <param name="ticket">The authentication ticket to serialize.</param>
    /// <returns>The serialized representation of <paramref name="ticket"/>.</returns>
    public virtual byte[] Serialize(AuthenticationTicket ticket)
    {
        // When this is exactly TicketSerializer, none of the virtual Write* methods can have been overridden,
        // so the payload can be written directly into a pooled buffer without a stream and writer.
        if (GetType() == typeof(TicketSerializer))
        {
            ArgumentNullException.ThrowIfNull(ticket);

            var payloadWriter = new BinaryPayloadWriter(initialCapacity: 1024);
            try
            {
                Write(ref payloadWriter, ticket);
                return payloadWriter.ToArray();
            }
            finally
            {
                payloadWriter.Dispose();
            }
        }

        using (var memory = new MemoryStream())
        {
            using (var writer = new BinaryWriter(memory))
            {
                Write(writer, ticket);
            }
            return memory.ToArray();
        }
    }

    /// <summary>
    /// Deserializes the specified authentication ticket payload.
    /// </summary>
    /// <param name="data">The serialized authentication ticket.</param>
    /// <returns>The deserialized <see cref="AuthenticationTicket"/>, or <see langword="null"/> if the format is unsupported.</returns>
    public virtual AuthenticationTicket? Deserialize(byte[] data)
    {
        // When this is exactly TicketSerializer, none of the virtual Read* methods can have been overridden,
        // so the payload can be read directly from the buffer without a stream and reader.
        if (GetType() == typeof(TicketSerializer))
        {
            // Matches the parameter name of the MemoryStream constructor used below.
            ArgumentNullException.ThrowIfNull(data, "buffer");

            var payloadReader = new BinaryPayloadReader(data);
            return Read(ref payloadReader);
        }

        using (var memory = new MemoryStream(data))
        {
            using (var reader = new BinaryReader(memory))
            {
                return Read(reader);
            }
        }
    }

    /// <summary>
    /// Writes the <paramref name="ticket"/> using the specified <paramref name="writer"/>.
    /// </summary>
    /// <param name="writer">The <see cref="BinaryWriter"/>.</param>
    /// <param name="ticket">The <see cref="AuthenticationTicket"/>.</param>
    public virtual void Write(BinaryWriter writer, AuthenticationTicket ticket)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(ticket);

        var payloadWriter = new BinaryPayloadWriter(writer);
        Write(ref payloadWriter, ticket);
    }

    // The private ref BinaryPayloadWriter/BinaryPayloadReader overloads below implement the format once for both the
    // public BinaryWriter/BinaryReader-based methods and the buffer-based fast paths. When the payload writer or reader
    // wraps a BinaryWriter or BinaryReader, nested identities and claims are dispatched through the virtual methods so
    // that derived serializers' overrides are honored.
    private void Write(ref BinaryPayloadWriter writer, AuthenticationTicket ticket)
    {
        writer.WriteInt32(FormatVersion);
        writer.WriteString(ticket.AuthenticationScheme);

        // Write the number of identities contained in the principal.
        var principal = ticket.Principal;
        writer.WriteInt32(principal.Identities.Count());

        foreach (var identity in principal.Identities)
        {
            WriteIdentity(ref writer, identity);
        }

        PropertiesSerializer.Write(ref writer, ticket.Properties);
    }

    /// <summary>
    /// Writes the specified <paramref name="identity" />.
    /// </summary>
    /// <param name="writer">The <see cref="BinaryWriter" />.</param>
    /// <param name="identity">The <see cref="ClaimsIdentity" />.</param>
    protected virtual void WriteIdentity(BinaryWriter writer, ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var payloadWriter = new BinaryPayloadWriter(writer);
        WriteIdentityCore(ref payloadWriter, identity);
    }

    private void WriteIdentity(ref BinaryPayloadWriter writer, ClaimsIdentity identity)
    {
        if (writer.BinaryWriter is { } binaryWriter)
        {
            WriteIdentity(binaryWriter, identity);
        }
        else
        {
            WriteIdentityCore(ref writer, identity);
        }
    }

    private void WriteIdentityCore(ref BinaryPayloadWriter writer, ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var authenticationType = identity.AuthenticationType ?? string.Empty;

        writer.WriteString(authenticationType);
        WriteWithDefault(ref writer, identity.NameClaimType, ClaimsIdentity.DefaultNameClaimType);
        WriteWithDefault(ref writer, identity.RoleClaimType, ClaimsIdentity.DefaultRoleClaimType);

        // Write the number of claims contained in the identity.
        writer.WriteInt32(identity.Claims.Count());

        foreach (var claim in identity.Claims)
        {
            WriteClaim(ref writer, claim);
        }

        var bootstrap = identity.BootstrapContext as string;
        if (!string.IsNullOrEmpty(bootstrap))
        {
            writer.WriteBoolean(true);
            writer.WriteString(bootstrap);
        }
        else
        {
            writer.WriteBoolean(false);
        }

        if (identity.Actor != null)
        {
            writer.WriteBoolean(true);
            WriteIdentity(ref writer, identity.Actor);
        }
        else
        {
            writer.WriteBoolean(false);
        }
    }

    /// <summary>
    /// Writes the specified claim using the provided binary writer.
    /// </summary>
    /// <param name="writer">The binary writer to write to.</param>
    /// <param name="claim">The claim to serialize.</param>
    protected virtual void WriteClaim(BinaryWriter writer, Claim claim)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var payloadWriter = new BinaryPayloadWriter(writer);
        WriteClaimCore(ref payloadWriter, claim);
    }

    private void WriteClaim(ref BinaryPayloadWriter writer, Claim claim)
    {
        if (writer.BinaryWriter is { } binaryWriter)
        {
            WriteClaim(binaryWriter, claim);
        }
        else
        {
            WriteClaimCore(ref writer, claim);
        }
    }

    private static void WriteClaimCore(ref BinaryPayloadWriter writer, Claim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        WriteWithDefault(ref writer, claim.Type, claim.Subject?.NameClaimType ?? ClaimsIdentity.DefaultNameClaimType);
        writer.WriteString(claim.Value);
        WriteWithDefault(ref writer, claim.ValueType, ClaimValueTypes.String);
        WriteWithDefault(ref writer, claim.Issuer, ClaimsIdentity.DefaultIssuer);
        WriteWithDefault(ref writer, claim.OriginalIssuer, claim.Issuer);

        // Write the number of properties contained in the claim.
        writer.WriteInt32(claim.Properties.Count);

        foreach (var property in claim.Properties)
        {
            writer.WriteString(property.Key ?? string.Empty);
            writer.WriteString(property.Value ?? string.Empty);
        }
    }

    /// <summary>
    /// Reads an <see cref="AuthenticationTicket"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/>.</param>
    /// <returns>The <see cref="AuthenticationTicket"/> if the format is supported, otherwise <see langword="null"/>.</returns>
    public virtual AuthenticationTicket? Read(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var payloadReader = new BinaryPayloadReader(reader);
        return Read(ref payloadReader);
    }

    private AuthenticationTicket? Read(ref BinaryPayloadReader reader)
    {
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
            identities[index] = ReadIdentity(ref reader);
        }

        var properties = PropertiesSerializer.Read(ref reader);

        return new AuthenticationTicket(new ClaimsPrincipal(identities), properties, scheme);
    }

    /// <summary>
    /// Reads a <see cref="ClaimsIdentity"/> from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/>.</param>
    /// <returns>The read <see cref="ClaimsIdentity"/>.</returns>
    protected virtual ClaimsIdentity ReadIdentity(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var payloadReader = new BinaryPayloadReader(reader);
        return ReadIdentityCore(ref payloadReader);
    }

    private ClaimsIdentity ReadIdentity(ref BinaryPayloadReader reader)
    {
        return reader.BinaryReader is { } binaryReader
            ? ReadIdentity(binaryReader)
            : ReadIdentityCore(ref reader);
    }

    private ClaimsIdentity ReadIdentityCore(ref BinaryPayloadReader reader)
    {
        var authenticationType = reader.ReadString();
        var nameClaimType = reader.ReadStringWithDefault(ClaimsIdentity.DefaultNameClaimType);
        var roleClaimType = reader.ReadStringWithDefault(ClaimsIdentity.DefaultRoleClaimType);

        // Read the number of claims contained
        // in the serialized identity.
        var count = reader.ReadInt32();

        var identity = new ClaimsIdentity(authenticationType, nameClaimType, roleClaimType);

        for (int index = 0; index != count; ++index)
        {
            var claim = ReadClaim(ref reader, identity);

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
            identity.Actor = ReadIdentity(ref reader);
        }

        return identity;
    }

    /// <summary>
    /// Reads a <see cref="Claim"/> and adds it to the specified <paramref name="identity"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/>.</param>
    /// <param name="identity">The <see cref="ClaimsIdentity"/> to add the claim to.</param>
    /// <returns>The read <see cref="Claim"/>.</returns>
    protected virtual Claim ReadClaim(BinaryReader reader, ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(identity);

        var payloadReader = new BinaryPayloadReader(reader);
        return ReadClaimCore(ref payloadReader, identity);
    }

    private Claim ReadClaim(ref BinaryPayloadReader reader, ClaimsIdentity identity)
    {
        return reader.BinaryReader is { } binaryReader
            ? ReadClaim(binaryReader, identity)
            : ReadClaimCore(ref reader, identity);
    }

    private static Claim ReadClaimCore(ref BinaryPayloadReader reader, ClaimsIdentity identity)
    {
        var type = reader.ReadStringWithDefault(identity.NameClaimType);
        var value = reader.ReadString();
        var valueType = reader.ReadStringWithDefault(ClaimValueTypes.String);
        var issuer = reader.ReadStringWithDefault(ClaimsIdentity.DefaultIssuer);
        var originalIssuer = reader.ReadStringWithDefault(issuer);

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

    private static void WriteWithDefault(ref BinaryPayloadWriter writer, string value, string defaultValue)
    {
        if (string.Equals(value, defaultValue, StringComparison.Ordinal))
        {
            writer.WriteString(DefaultStringPlaceholder);
        }
        else
        {
            writer.WriteString(value);
        }
    }
}
