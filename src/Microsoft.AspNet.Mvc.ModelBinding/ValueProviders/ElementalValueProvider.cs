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
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    // Represents a value provider that contains a single value.
    internal sealed class ElementalValueProvider : IValueProvider
    {
        public ElementalValueProvider(string name, object rawValue, CultureInfo culture)
        {
            Name = name;
            RawValue = rawValue;
            Culture = culture;
        }

        public CultureInfo Culture { get; private set; }

        public string Name { get; private set; }

        public object RawValue { get; private set; }

        public Task<bool> ContainsPrefixAsync(string prefix)
        {
            return Task.FromResult(PrefixContainer.IsPrefixMatch(Name, prefix));
        }

        public Task<ValueProviderResult> GetValueAsync(string key)
        {
            var result = string.Equals(key, Name, StringComparison.OrdinalIgnoreCase) ?
                                new ValueProviderResult(RawValue, Convert.ToString(RawValue, Culture), Culture) :
                                null;
            return Task.FromResult(result);
        }
    }
}
