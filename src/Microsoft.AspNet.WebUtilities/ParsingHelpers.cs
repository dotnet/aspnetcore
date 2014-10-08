// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.WebUtilities.Collections;

namespace Microsoft.AspNet.WebUtilities
{
    internal static class ParsingHelpers
    {
        internal static void ParseDelimited(string text, char[] delimiters, Action<string, string, object> callback, object state)
        {
            int textLength = text.Length;
            int equalIndex = text.IndexOf('=');
            if (equalIndex == -1)
            {
                equalIndex = textLength;
            }
            int scanIndex = 0;
            while (scanIndex < textLength)
            {
                int delimiterIndex = text.IndexOfAny(delimiters, scanIndex);
                if (delimiterIndex == -1)
                {
                    delimiterIndex = textLength;
                }
                if (equalIndex < delimiterIndex)
                {
                    while (scanIndex != equalIndex && char.IsWhiteSpace(text[scanIndex]))
                    {
                        ++scanIndex;
                    }
                    string name = text.Substring(scanIndex, equalIndex - scanIndex);
                    string value = text.Substring(equalIndex + 1, delimiterIndex - equalIndex - 1);
                    callback(
                        Uri.UnescapeDataString(name.Replace('+', ' ')),
                        Uri.UnescapeDataString(value.Replace('+', ' ')),
                        state);
                    equalIndex = text.IndexOf('=', delimiterIndex);
                    if (equalIndex == -1)
                    {
                        equalIndex = textLength;
                    }
                }
                scanIndex = delimiterIndex + 1;
            }
        }

        private static readonly Action<string, string, object> AppendItemCallback = (name, value, state) =>
        {
            var dictionary = (IDictionary<string, List<String>>)state;

            List<string> existing;
            if (!dictionary.TryGetValue(name, out existing))
            {
                dictionary.Add(name, new List<string>(1) { value });
            }
            else
            {
                existing.Add(value);
            }
        };

        internal static IFormCollection GetForm(string text)
        {
            IDictionary<string, string[]> form = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
            var accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            ParseDelimited(text, Ampersand, AppendItemCallback, accumulator);
            foreach (var kv in accumulator)
            {
                form.Add(kv.Key, kv.Value.ToArray());
            }
            return new FormCollection(form);
        }

        internal static string GetJoinedValue(IDictionary<string, string[]> store, string key)
        {
            string[] values = GetUnmodifiedValues(store, key);
            return values == null ? null : string.Join(",", values);
        }

        internal static string[] GetUnmodifiedValues(IDictionary<string, string[]> store, string key)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }
            string[] values;
            return store.TryGetValue(key, out values) ? values : null;
        }

        private static readonly char[] Ampersand = new[] { '&' };

        internal static IReadableStringCollection GetQuery(string queryString)
        {
            if (!string.IsNullOrEmpty(queryString) && queryString[0] == '?')
            {
                queryString = queryString.Substring(1);
            }
            var accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            ParseDelimited(queryString, Ampersand, AppendItemCallback, accumulator);
            return new ReadableStringCollection(accumulator.ToDictionary(
                    item => item.Key,
                    item => item.Value.ToArray(),
                    StringComparer.OrdinalIgnoreCase));
        }
    }
}
