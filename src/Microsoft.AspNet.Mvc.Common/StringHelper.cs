// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;

namespace Microsoft.AspNet.Mvc
{
    internal static class StringHelper
    {
        public static string TrimSpacesAndChars(string value, params char[] chars)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (chars == null || chars.Length == 0)
            {
                return value.Trim();
            }

            var firstIndex = 0;
            for (; firstIndex < value.Length; firstIndex++)
            {
                var currentChar = value[firstIndex];
                if (!char.IsWhiteSpace(currentChar) && !chars.Any(compareChar => compareChar == currentChar))
                {
                    break;
                }
            }

            // We trimmed all the way
            if (firstIndex == value.Length)
            {
                return string.Empty;
            }

            var lastIndex = value.Length - 1;
            for (; lastIndex > firstIndex; lastIndex--)
            {
                var currentChar = value[lastIndex];
                if (!char.IsWhiteSpace(currentChar) && !chars.Any(compareChar => compareChar == currentChar))
                {
                    break;
                }
            }

            if (firstIndex == 0 && lastIndex == value.Length - 1)
            {
                return value;
            }
            else
            {
                return value.Substring(firstIndex, lastIndex - firstIndex + 1);
            }
        }
    }
}