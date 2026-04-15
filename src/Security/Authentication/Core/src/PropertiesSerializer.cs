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

    /// <summary>
    /// Serializes the specified authentication properties.
    /// </summary>
    /// <param name="model">The authentication properties to serialize.</param>
    /// <returns>The serialized representation of <paramref name="model"/>.</returns>
    public virtual byte[] Serialize(AuthenticationProperties model)
    {
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

    /// <summary>
    /// Deserializes the specified authentication properties payload.
    /// </summary>
    /// <param name="data">The serialized authentication properties.</param>
    /// <returns>The deserialized <see cref="AuthenticationProperties"/>, or <see langword="null"/> if the format is unsupported.</returns>
    public virtual AuthenticationProperties? Deserialize(byte[] data)
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
    /// Writes the specified authentication properties to the provided binary writer.
    /// </summary>
    /// <param name="writer">The binary writer to write to.</param>
    /// <param name="properties">The authentication properties to write.</param>
    public virtual void Write(BinaryWriter writer, AuthenticationProperties properties)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(properties);

        writer.Write(FormatVersion);
        writer.Write(properties.Items.Count);

        foreach (var item in properties.Items)
        {
            writer.Write(item.Key ?? string.Empty);
            writer.Write(item.Value ?? string.Empty);
        }
    }

    /// <summary>
    /// Reads authentication properties from the provided binary reader.
    /// </summary>
    /// <param name="reader">The binary reader to read from.</param>
    /// <returns>The deserialized <see cref="AuthenticationProperties"/>, or <see langword="null"/> if the format is unsupported.</returns>
    public virtual AuthenticationProperties? Read(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

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
