// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding.Test
{
    public sealed class SimpleValueProvider : Dictionary<string, object>, IValueProvider
    {
        private readonly CultureInfo _culture;

        public SimpleValueProvider()
            : this(null)
        {
        }

        public SimpleValueProvider(CultureInfo culture)
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
            ValueProviderResult result = ValueProviderResult.None;

            object rawValue;
            if (TryGetValue(key, out rawValue))
            {
                if (rawValue != null && rawValue.GetType().IsArray)
                {
                    var array = (Array)rawValue;

                    var stringValues = new string[array.Length];
                    for (var i = 0; i < array.Length; i++)
                    {
                        stringValues[i] = array.GetValue(i) as string ?? Convert.ToString(array.GetValue(i), _culture);
                    }

                    result = new ValueProviderResult(stringValues, _culture);
                }
                else
                {
                    var stringValue = rawValue as string ?? Convert.ToString(rawValue, _culture) ?? string.Empty;
                    result = new ValueProviderResult(stringValue, _culture);
                }
            }

            return Task.FromResult(result);
        }
    }
}
