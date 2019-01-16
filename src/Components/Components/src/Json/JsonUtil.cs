// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSInterop;
using System;

namespace Microsoft.AspNetCore.Components
{
    /// <summary>
    /// Provides mechanisms for converting between .NET objects and JSON strings.
    /// </summary>
    [Obsolete("Use Microsoft.JSInterop.Json instead.")]
    public static class JsonUtil
    {
        /// <summary>
        /// Serializes the value as a JSON string.
        /// </summary>
        /// <param name="value">The value to serialize.</param>
        /// <returns>The JSON string.</returns>
        [Obsolete("Use Microsoft.JSInterop.Json.Serialize instead.")]
        public static string Serialize(object value)
            => Json.Serialize(value);

        /// <summary>
        /// Deserializes the JSON string, creating an object of the specified generic type.
        /// </summary>
        /// <typeparam name="T">The type of object to create.</typeparam>
        /// <param name="json">The JSON string.</param>
        /// <returns>An object of the specified type.</returns>
        [Obsolete("Use Microsoft.JSInterop.Json.Deserialize<T> instead.")]
        public static T Deserialize<T>(string json)
            => Json.Deserialize<T>(json);
    }
}
