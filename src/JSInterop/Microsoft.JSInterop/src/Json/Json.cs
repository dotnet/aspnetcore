// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.JSInterop
{
    /// <summary>
    /// Provides mechanisms for converting between .NET objects and JSON strings for use
    /// when making calls to JavaScript functions via <see cref="IJSRuntime"/>.
    /// 
    /// Warning: This is not intended as a general-purpose JSON library. It is only intended
    /// for use when making calls via <see cref="IJSRuntime"/>. Eventually its implementation
    /// will be replaced by something more general-purpose.
    /// </summary>
    public static class Json
    {
        /// <summary>
        /// Serializes the value as a JSON string.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The JSON string.</returns>
        public static string Serialize(object value)
            => SimpleJson.SimpleJson.SerializeObject(value);

        internal static string Serialize(object value, SimpleJson.IJsonSerializerStrategy serializerStrategy)
            => SimpleJson.SimpleJson.SerializeObject(value, serializerStrategy);

        /// <summary>
        /// Deserializes the JSON string, creating an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type of object to create.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <returns>An object of the specified type.</returns>
        public static T Deserialize<T>(string json)
            => SimpleJson.SimpleJson.DeserializeObject<T>(json);

        internal static T Deserialize<T>(string json, SimpleJson.IJsonSerializerStrategy serializerStrategy)
            => SimpleJson.SimpleJson.DeserializeObject<T>(json, serializerStrategy);
    }
}
