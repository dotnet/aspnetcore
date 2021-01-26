// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Authentication extensions to <see cref="JsonDocument"/>.
    /// </summary>
    public static class JsonDocumentAuthExtensions
    {
        /// <summary>
        /// Gets a string property value from the specified <see cref="JsonElement"/>.
        /// </summary>
        /// <param name="element">The <see cref="JsonElement"/>.</param>
        /// <param name="key">The property name.</param>
        /// <returns>The property value.</returns>
        public static string? GetString(this JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out var property) && property.ValueKind != JsonValueKind.Null)
            {
                return property.ToString();
            }

            return null;
        }
    }
}
