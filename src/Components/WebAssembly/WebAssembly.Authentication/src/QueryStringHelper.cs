// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    internal class QueryStringHelper
    {
        public static string GetParameter(string queryString, string key)
        {
            if (string.IsNullOrEmpty(queryString) || queryString == "?")
            {
                return null;
            }

            var scanIndex = 0;
            if (queryString[0] == '?')
            {
                scanIndex = 1;
            }

            var textLength = queryString.Length;
            var equalIndex = queryString.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }

            while (scanIndex < textLength)
            {
                var ampersandIndex = queryString.IndexOf('&', scanIndex);
                if (ampersandIndex == -1)
                {
                    ampersandIndex = textLength;
                }

                if (equalIndex < ampersandIndex)
                {
                    while (scanIndex != equalIndex && char.IsWhiteSpace(queryString[scanIndex]))
                    {
                        ++scanIndex;
                    }
                    var name = queryString[scanIndex..equalIndex];
                    var value = queryString.Substring(equalIndex + 1, ampersandIndex - equalIndex - 1);
                    var processedName = Uri.UnescapeDataString(name.Replace('+', ' '));
                    if (string.Equals(processedName, key, StringComparison.OrdinalIgnoreCase))
                    {
                        return Uri.UnescapeDataString(value.Replace('+', ' '));
                    }

                    equalIndex = queryString.IndexOf('=', ampersandIndex);
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
                else
                {
                    if (ampersandIndex > scanIndex)
                    {
                        var value = queryString[scanIndex..ampersandIndex];
                        if (string.Equals(value, key, StringComparison.OrdinalIgnoreCase))
                        {
                            return string.Empty;
                        }
                    }
                }

                scanIndex = ampersandIndex + 1;
            }

            return null;
        }
    }
}
