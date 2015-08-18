// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ElementalValueProvider : IValueProvider
    {
        public ElementalValueProvider(string key, string value, CultureInfo culture)
        {
            Key = key;
            Value = value;
            Culture = culture;
        }

        public CultureInfo Culture { get; }

        public string Key { get; }

        public string Value { get; }

        public Task<bool> ContainsPrefixAsync(string prefix)
        {
            return Task.FromResult(PrefixContainer.IsPrefixMatch(prefix, Key));
        }

        public Task<ValueProviderResult> GetValueAsync(string key)
        {
            if (string.Equals(key, Key, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new ValueProviderResult(Value, Culture));
            }
            else
            {
                return Task.FromResult(ValueProviderResult.None);
            }
        }
    }
}
