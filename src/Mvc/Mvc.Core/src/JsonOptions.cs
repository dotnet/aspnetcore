// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Microsoft.AspNetCore.Mvc
{
    /// <summary>
    /// Options to configure <see cref="SystemTextJsonInputFormatter"/> and <see cref="SystemTextJsonOutputFormatter"/>.
    /// </summary>
    public class JsonOptions
    {
        internal CompositeJsonserializerContext CompositeContext = new();

        /// <summary>
        /// Gets or sets a flag to determine whether error messages from JSON deserialization by the
        /// <see cref="SystemTextJsonInputFormatter"/> will be added to the <see cref="ModelStateDictionary"/>. If
        /// <see langword="false"/>, a generic error message will be used instead.
        /// </summary>
        /// <value>
        /// The default value is <see langword="true"/>.
        /// </value>
        /// <remarks>
        /// Error messages in the <see cref="ModelStateDictionary"/> are often communicated to clients, either in HTML
        /// or using <see cref="BadRequestObjectResult"/>. In effect, this setting controls whether clients can receive
        /// detailed error messages about submitted JSON data.
        /// </remarks>
        public bool AllowInputFormatterExceptionMessages { get; set; } = true;
        
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

        /// <summary>
        /// Configures a <see cref="JsonSerializerContext"/> to be used during JSON serialization and deserialization.
        /// </summary>
        /// <param name="jsonSerializerContext">The <see cref="JsonSerializerContext"/>.</param>
        public void AddJsonSerializerContext(JsonSerializerContext jsonSerializerContext) => CompositeContext.Add(jsonSerializerContext);
    }
}
