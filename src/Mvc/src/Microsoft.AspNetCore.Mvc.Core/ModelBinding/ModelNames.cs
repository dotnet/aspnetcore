// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    public static class ModelNames
    {
        public static string CreateIndexModelName(string parentName, int index)
        {
            return CreateIndexModelName(parentName, index.ToString(CultureInfo.InvariantCulture));
        }

        public static string CreateIndexModelName(string parentName, string index)
        {
            return (parentName.Length == 0) ? "[" + index + "]" : parentName + "[" + index + "]";
        }

        public static string CreatePropertyModelName(string prefix, string propertyName)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return propertyName ?? string.Empty;
            }

            if (string.IsNullOrEmpty(propertyName))
            {
                return prefix ?? string.Empty;
            }

            if (propertyName.StartsWith("[", StringComparison.Ordinal))
            {
                // The propertyName might represent an indexer access, in which case combining
                // with a 'dot' would be invalid. This case occurs only when called from ValidationVisitor.
                return prefix + propertyName;
            }

            return prefix + "." + propertyName;
        }
    }
}
