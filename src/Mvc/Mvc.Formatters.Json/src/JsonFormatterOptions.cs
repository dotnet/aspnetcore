// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json.Serialization;

namespace Microsoft.AspNetCore.Mvc.Formatters
{
    /// <summary>
    /// Provides options to configure formatters using <c>System.Text.Json</c>.
    /// </summary>
    public class JsonFormatterOptions
    {
        /// <summary>
        /// Gets the <see cref="JsonSerializerOptions"/>.
        /// </summary>
        public JsonSerializerOptions SerializerOptions { get; } = new JsonSerializerOptions
        {
            IgnoreNullPropertyValueOnRead = true,
        };
    }
}
