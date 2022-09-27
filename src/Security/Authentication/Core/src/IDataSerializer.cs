// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Authentication;

/// <summary>
/// Contract for serialzing authentication data.
/// </summary>
/// <typeparam name="TModel">The type of the model being serialized.</typeparam>
public interface IDataSerializer<TModel>
{
    /// <summary>
    /// Serializes the specified <paramref name="model"/>.
    /// </summary>
    /// <param name="model">The value to serialize.</param>
    /// <returns>The serialized data.</returns>
    byte[] Serialize(TModel model);

    /// <summary>
    /// Deserializes the specified <paramref name="data"/> as an instance of type <typeparamref name="TModel"/>.
    /// </summary>
    /// <param name="data">The bytes being deserialized.</param>
    /// <returns>The model.</returns>
    TModel? Deserialize(byte[] data);
}
