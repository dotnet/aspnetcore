// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.JsonPatch.Internal
{
    // Helper methods to allow case-insensitive key search
    public static class ExpandoObjectDictionaryExtensions
    {
        internal static string GetKeyUsingCaseInsensitiveSearch(
            this IDictionary<string, object> propertyDictionary,
            string key)
        {
            foreach (var keyInDictionary in propertyDictionary.Keys)
            {
                if (string.Equals(key, keyInDictionary, StringComparison.OrdinalIgnoreCase))
                {
                    return keyInDictionary;
                }
            }
            return key;
        }
    }
}