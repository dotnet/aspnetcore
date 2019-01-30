// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Internal
{
    public static class ParsingHelpers
    {
        public static StringValues GetHeader(IHeaderDictionary headers, string key)
        {
            StringValues value;
            return headers.TryGetValue(key, out value) ? value : StringValues.Empty;
        }

        public static StringValues GetHeaderSplit(IHeaderDictionary headers, string key)
        {
            var values = GetHeaderUnmodified(headers, key);
            return new StringValues(GetHeaderSplitImplementation(values).ToArray());
        }

        private static IEnumerable<string> GetHeaderSplitImplementation(StringValues values)
        {
            foreach (var segment in new HeaderSegmentCollection(values))
            {
                if (!StringSegment.IsNullOrEmpty(segment.Data))
                {
                    var value = DeQuote(segment.Data.Value);
                    if (!string.IsNullOrEmpty(value))
                    {
                        yield return value;
                    }
                }
            }
        }

        public static StringValues GetHeaderUnmodified(IHeaderDictionary headers, string key)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            StringValues values;
            return headers.TryGetValue(key, out values) ? values : StringValues.Empty;
        }

        public static void SetHeaderJoined(IHeaderDictionary headers, string key, StringValues value)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (StringValues.IsNullOrEmpty(value))
            {
                headers.Remove(key);
            }
            else
            {
                headers[key] = string.Join(",", value.Select((s) => QuoteIfNeeded(s)));
            }
        }

        // Quote items that contain commas and are not already quoted.
        private static string QuoteIfNeeded(string value)
        {
            if (!string.IsNullOrEmpty(value) &&
                value.Contains(',') &&
                (value[0] != '"' || value[value.Length - 1] != '"'))
            {
                return $"\"{value}\"";
            }
            return value;
        }

        private static string DeQuote(string value)
        {
            if (!string.IsNullOrEmpty(value) &&
                (value.Length > 1 && value[0] == '"' && value[value.Length - 1] == '"'))
            {
                value = value.Substring(1, value.Length - 2);
            }

            return value;
        }

        public static void SetHeaderUnmodified(IHeaderDictionary headers, string key, StringValues? values)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (!values.HasValue || StringValues.IsNullOrEmpty(values.GetValueOrDefault()))
            {
                headers.Remove(key);
            }
            else
            {
                headers[key] = values.GetValueOrDefault();
            }
        }

        public static void AppendHeaderJoined(IHeaderDictionary headers, string key, params string[] values)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (values == null || values.Length == 0)
            {
                return;
            }

            string existing = GetHeader(headers, key);
            if (existing == null)
            {
                SetHeaderJoined(headers, key, values);
            }
            else
            {
                headers[key] = existing + "," + string.Join(",", values.Select(value => QuoteIfNeeded(value)));
            }
        }

        public static void AppendHeaderUnmodified(IHeaderDictionary headers, string key, StringValues values)
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (values.Count == 0)
            {
                return;
            }

            var existing = GetHeaderUnmodified(headers, key);
            SetHeaderUnmodified(headers, key, StringValues.Concat(existing, values));
        }
    }
}
