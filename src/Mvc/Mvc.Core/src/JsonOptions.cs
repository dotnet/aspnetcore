// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Options to configure <see cref="SystemTextJsonInputFormatter"/> and <see cref="SystemTextJsonOutputFormatter"/>.
    /// </summary>
    public class JsonOptions
    {
        /// <summary>
        /// Gets the <see cref="System.Text.Json.JsonSerializerOptions"/> used by <see cref="SystemTextJsonInputFormatter"/> and
        /// <see cref="SystemTextJsonOutputFormatter"/>.
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            // Limit the object graph we'll consume to a fixed depth. This prevents stackoverflow exceptions
            // from deserialization errors that might occur from deeply nested objects.
            // This value is the same for model binding and Json.Net's serialization.
            MaxDepth = MvcOptions.DefaultMaxModelBindingRecursionDepth,
        };
    }
}
