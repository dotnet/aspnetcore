// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNet.WebUtilities
{
    public struct KeyValueAccumulator
    {
        private Dictionary<string, List<string>> _accumulator;

        public void Append(string key, string value)
        {
            if (_accumulator == null)
            {
                _accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            }
            List<string> values;
            if (_accumulator.TryGetValue(key, out values))
            {
                values.Add(value);
            }
            else
            {
                values = new List<string>(1);
                values.Add(value);
                _accumulator[key] = values;
            }
        }

        public bool HasValues => _accumulator != null;

        public Dictionary<string, StringValues> GetResults()
        {
            if (_accumulator == null)
            {
                return new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            }

            var results = new Dictionary<string, StringValues>(_accumulator.Count, StringComparer.OrdinalIgnoreCase);

            foreach (var kv in _accumulator)
            {
                results.Add(kv.Key, kv.Value.Count == 1 ? new StringValues(kv.Value[0]) : new StringValues(kv.Value.ToArray()));
            }

            return results;
        }
    }
}
