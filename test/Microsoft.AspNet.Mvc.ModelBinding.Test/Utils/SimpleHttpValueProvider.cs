// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public sealed class SimpleHttpValueProvider : Dictionary<string, object>, IValueProvider
    {
        private readonly CultureInfo _culture;

        public SimpleHttpValueProvider()
            : this(null)
        {
        }

        public SimpleHttpValueProvider(CultureInfo culture)
            : base(StringComparer.OrdinalIgnoreCase)
        {
            _culture = culture ?? CultureInfo.InvariantCulture;
        }

        // copied from ValueProviderUtil
        public Task<bool> ContainsPrefixAsync(string prefix)
        {
            foreach (string key in Keys)
            {
                if (key != null)
                {
                    if (prefix.Length == 0)
                    {
                        return Task.FromResult(true); // shortcut - non-null key matches empty prefix
                    }

                    if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        if (key.Length == prefix.Length)
                        {
                            return Task.FromResult(true); // exact match
                        }
                        else
                        {
                            switch (key[prefix.Length])
                            {
                                case '.': // known separator characters
                                case '[':
                                    return Task.FromResult(true);
                            }
                        }
                    }
                }
            }

            return Task.FromResult(false); // nothing found
        }

        public Task<ValueProviderResult> GetValueAsync(string key)
        {
            ValueProviderResult result = null;
            object rawValue;
            if (TryGetValue(key, out rawValue))
            {
                result = new ValueProviderResult(rawValue, Convert.ToString(rawValue, _culture), _culture);
            }

            return Task.FromResult(result);
        }
    }
}
