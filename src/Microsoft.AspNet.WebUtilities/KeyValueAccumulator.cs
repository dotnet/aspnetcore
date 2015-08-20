// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.Framework.Primitives;

namespace Microsoft.AspNet.WebUtilities
{
    public class KeyValueAccumulator
    {
        private Dictionary<string, List<string>> _accumulator;

        public KeyValueAccumulator()
        {
            _accumulator = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        }

        public void Append(string key, string value)
        {
            List<string> values;
            if (_accumulator.TryGetValue(key, out values))
            {
                values.Add(value);
            }
            else
            {
                _accumulator[key] = new List<string>(1) { value };
            }
        }

        public IDictionary<string, StringValues> GetResults()
        {
            var results = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in _accumulator)
            {
                results.Add(kv.Key, kv.Value.ToArray());
            }
            return results;
        }
    }
}