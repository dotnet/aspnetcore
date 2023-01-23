// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication.Twitter;

/// <summary>
/// Serializes and deserializes Twitter request and access tokens so that they can be used by other application components.
/// </summary>
public class RequestTokenSerializer : IDataSerializer<RequestToken>
{
    private const int FormatVersion = 1;

    /// <summary>
    /// Serialize a request token.
    /// </summary>
    /// <param name="model">The token to serialize</param>
    /// <returns>A byte array containing the serialized token</returns>
    public virtual byte[] Serialize(RequestToken model)
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
    /// Deserializes a request token.
    /// </summary>
    /// <param name="data">A byte array containing the serialized token</param>
    /// <returns>The Twitter request token</returns>
    public virtual RequestToken? Deserialize(byte[] data)
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
    /// Writes a Twitter request token as a series of bytes. Used by the <see cref="Serialize"/> method.
    /// </summary>
    /// <param name="writer">The writer to use in writing the token</param>
    /// <param name="token">The token to write</param>
    public static void Write(BinaryWriter writer, RequestToken token)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(token);

        writer.Write(FormatVersion);
        writer.Write(token.Token);
        writer.Write(token.TokenSecret);
        writer.Write(token.CallbackConfirmed);
        PropertiesSerializer.Default.Write(writer, token.Properties);
    }

    /// <summary>
    /// Reads a Twitter request token from a series of bytes. Used by the <see cref="Deserialize"/> method.
    /// </summary>
    /// <param name="reader">The reader to use in reading the token bytes</param>
    /// <returns>The token</returns>
    public static RequestToken? Read(BinaryReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        if (reader.ReadInt32() != FormatVersion)
        {
            return null;
        }

        string token = reader.ReadString();
        string tokenSecret = reader.ReadString();
        bool callbackConfirmed = reader.ReadBoolean();
        AuthenticationProperties? properties = PropertiesSerializer.Default.Read(reader);
        if (properties == null)
        {
            return null;
        }

        return new RequestToken { Token = token, TokenSecret = tokenSecret, CallbackConfirmed = callbackConfirmed, Properties = properties };
    }
}
