// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.AspNetCore.Authentication
{
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
}
