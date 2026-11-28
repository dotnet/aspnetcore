// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// A <see cref="IDataSerializer{TModel}"/> for <see cref="AuthenticationProperties"/>.
/// </summary>
public class PropertiesSerializer : IDataSerializer<AuthenticationProperties>
{
    private const int FormatVersion = 1;

    /// <summary>
    /// Gets the default instance of <see cref="PropertiesSerializer"/>.
    /// </summary>
    public static PropertiesSerializer Default { get; } = new PropertiesSerializer();

    /// <inheritdoc />
    public virtual byte[] Serialize(AuthenticationProperties model)
    {
        // When this is exactly PropertiesSerializer, Write cannot have been overridden,
        // so the payload can be written directly into a pooled buffer without a stream and writer.
        if (GetType() == typeof(PropertiesSerializer))
        {
            var payloadWriter = new BinaryPayloadWriter(initialCapacity: 256);
            try
            {
                Write(ref payloadWriter, model);
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
                Write(writer, model);
                writer.Flush();
                return memory.ToArray();
            }
        }
    }

    /// <inheritdoc />
    public virtual AuthenticationProperties? Deserialize(byte[] data)
    {
        // When this is exactly PropertiesSerializer, Read cannot have been overridden,
        // so the payload can be read directly from the buffer without a stream and reader.
        if (GetType() == typeof(PropertiesSerializer))
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

    /// <inheritdoc />
    public virtual void Write(BinaryWriter writer, AuthenticationProperties properties)
    {
        ArgumentNullException.ThrowIfNull(writer);

        var payloadWriter = new BinaryPayloadWriter(writer);
        Write(ref payloadWriter, properties);
    }

    internal static void Write(ref BinaryPayloadWriter writer, AuthenticationProperties properties)
    {
        ArgumentNullException.ThrowIfNull(properties);

        writer.WriteInt32(FormatVersion);
        writer.WriteInt32(properties.Items.Count);

        foreach (var item in properties.Items)
        {
            writer.WriteString(item.Key ?? string.Empty);
            writer.WriteString(item.Value ?? string.Empty);
        }
    }

    /// <inheritdoc />
    public virtual AuthenticationProperties? Read(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var payloadReader = new BinaryPayloadReader(reader);
        return Read(ref payloadReader);
    }

    internal static AuthenticationProperties? Read(ref BinaryPayloadReader reader)
    {
        if (reader.ReadInt32() != FormatVersion)
        {
            return null;
        }

        var count = reader.ReadInt32();
        var extra = new Dictionary<string, string?>(count);

        for (var index = 0; index != count; ++index)
        {
            var key = reader.ReadString();
            var value = reader.ReadString();
            extra.Add(key, value);
        }
        return new AuthenticationProperties(extra);
    }
}
