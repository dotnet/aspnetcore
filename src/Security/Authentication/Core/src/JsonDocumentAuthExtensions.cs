// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication
{
    public static class JsonDocumentAuthExtensions
    {
        public static string GetString(this JsonElement element, string key)
        {
            if (element.TryGetProperty(key, out var property) && property.ValueKind != JsonValueKind.Null)
            {
                return property.ToString();
            }

            return null;
        }
    }
}
