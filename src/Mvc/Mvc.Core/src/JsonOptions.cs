// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Microsoft.AspNetCore.Mvc
{
    public class JsonOptions
    {
        /// <summary>
        /// Gets the <see cref="System.Text.Json.JsonSerializerOptions"/> used by <see cref="SystemTextJsonInputFormatter"/> and
        /// <see cref="SystemTextJsonOutputFormatter"/>.
        /// </summary>
        public JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions
        {
            // Limit the object graph we'll consume to a fixed depth. This prevents stackoverflow exceptions
            // from deserialization errors that might occur from deeply nested objects.
            // This value is the same for model binding and Json.Net's serialization.
            MaxDepth = MvcOptions.DefaultMaxModelBindingRecursionDepth,

            // We're using case-insensitive because there's a TON of code that there that does uses JSON.NET's default
            // settings (preserve case) - including the WebAPIClient. This worked when we were using JSON.NET + camel casing
            // because JSON.NET is case-insensitive by default.
            PropertyNameCaseInsensitive = true,

            // Use camel casing for properties
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };
    }
}
