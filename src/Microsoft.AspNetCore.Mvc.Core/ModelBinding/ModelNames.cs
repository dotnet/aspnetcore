// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;

namespace Microsoft.AspNet.Mvc.ModelBinding
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
            else if (string.IsNullOrEmpty(propertyName))
            {
                return prefix ?? string.Empty;
            }
            else
            {
                return prefix + "." + propertyName;
            }
        }
    }
}
