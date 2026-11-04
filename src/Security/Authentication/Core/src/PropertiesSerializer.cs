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
        ArgumentNullException.ThrowIfNull(properties);

        writer.Write(FormatVersion);
        writer.Write(properties.Items.Count);

        foreach (var item in properties.Items)
        {
            writer.Write(item.Key ?? string.Empty);
            writer.Write(item.Value ?? string.Empty);
        }
    }

    /// <inheritdoc />
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
