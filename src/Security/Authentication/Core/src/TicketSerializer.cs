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
    private const string DefaultStringPlaceholder = "\0";
    private const int FormatVersion = 5;

    /// <summary>
    /// Gets the default implementation for <see cref="TicketSerializer"/>.
    /// </summary>
    public static TicketSerializer Default { get; } = new TicketSerializer();

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public virtual AuthenticationTicket? Deserialize(byte[] data)
    {
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

    /// <summary>
    /// Writes the specified <paramref name="identity" />.
    /// </summary>
    /// <param name="writer">The <see cref="BinaryWriter" />.</param>
    /// <param name="identity">The <see cref="ClaimsIdentity" />.</param>
    protected virtual void WriteIdentity(BinaryWriter writer, ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(identity);

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

    /// <inheritdoc/>
    protected virtual void WriteClaim(BinaryWriter writer, Claim claim)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(claim);

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

    /// <summary>
    /// Reads an <see cref="AuthenticationTicket"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/>.</param>
    /// <returns>The <see cref="AuthenticationTicket"/> if the format is supported, otherwise <see langword="null"/>.</returns>
    public virtual AuthenticationTicket? Read(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

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

    /// <summary>
    /// Reads a <see cref="ClaimsIdentity"/> from a <see cref="BinaryReader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="BinaryReader"/>.</param>
    /// <returns>The read <see cref="ClaimsIdentity"/>.</returns>
    protected virtual ClaimsIdentity ReadIdentity(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

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
