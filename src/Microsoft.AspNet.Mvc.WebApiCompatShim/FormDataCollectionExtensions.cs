// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http.Formatting;
using System.Text;

namespace Microsoft.AspNet.Mvc.WebApiCompatShim
{
    public static class FormDataCollectionExtensions
    {
        // This is a helper method to use Model Binding over a JQuery syntax.
        // Normalize from JQuery to MVC keys. The model binding infrastructure uses MVC keys
        // x[] --> x
        // [] --> ""
        // x[field]  --> x.field, where field is not a number
        public static string NormalizeJQueryToMvc(string key)
        {
            if (key == null)
            {
                return string.Empty;
            }

            StringBuilder sb = null;
            var i = 0;
            while (true)
            {
                var indexOpen = key.IndexOf('[', i);
                if (indexOpen < 0)
                {
                    // Fast path, no normalization needed.
                    // This skips the string conversion and allocating the string builder.
                    if (i == 0)
                    {
                        return key;
                    }
                    sb = sb ?? new StringBuilder();
                    sb.Append(key, i, key.Length - i);
                    break; // no more brackets
                }

                sb = sb ?? new StringBuilder();
                sb.Append(key, i, indexOpen - i); // everything up to "["

                // Find closing bracket.
                var indexClose = key.IndexOf(']', indexOpen);
                if (indexClose == -1)
                {
                    throw new ArgumentException(Resources.JQuerySyntaxMissingClosingBracket, "key");
                }

                if (indexClose == indexOpen + 1)
                {
                    // Empty bracket. Signifies array. Just remove.
                }
                else
                {
                    if (char.IsDigit(key[indexOpen + 1]))
                    {
                        // array index. Leave unchanged.
                        sb.Append(key, indexOpen, indexClose - indexOpen + 1);
                    }
                    else
                    {
                        // Field name.  Convert to dot notation.
                        sb.Append('.');
                        sb.Append(key, indexOpen + 1, indexClose - indexOpen - 1);
                    }
                }

                i = indexClose + 1;
                if (i >= key.Length)
                {
                    break; // end of string
                }
            }
            return sb.ToString();
        }

        public static IEnumerable<KeyValuePair<string, string>> GetJQueryNameValuePairs(
            [NotNull] this FormDataCollection formData)
        {
            var count = 0;

            foreach (var kv in formData)
            {
                ThrowIfMaxHttpCollectionKeysExceeded(count);

                var key = NormalizeJQueryToMvc(kv.Key);
                var value = kv.Value ?? string.Empty;
                yield return new KeyValuePair<string, string>(key, value);

                count++;
            }
        }

        private static void ThrowIfMaxHttpCollectionKeysExceeded(int count)
        {
            if (count >= MediaTypeFormatter.MaxHttpCollectionKeys)
            {
                var message = Resources.FormatMaxHttpCollectionKeyLimitReached(
                    MediaTypeFormatter.MaxHttpCollectionKeys,
                    typeof(MediaTypeFormatter));
                throw new InvalidOperationException(message);
            }
        }
    }
}
