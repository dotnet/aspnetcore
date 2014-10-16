// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DictionaryBasedValueProvider<TBinderMarker> : MarkerAwareValueProvider<TBinderMarker>
        where TBinderMarker : IValueBinderMarker
    {
        private readonly IDictionary<string, object> _values;

        public DictionaryBasedValueProvider(IDictionary<string, object> values)
        {
            _values = values;
        }

        public override Task<bool> ContainsPrefixAsync(string key)
        {
            return Task.FromResult(_values.ContainsKey(key));
        }

        public override Task<ValueProviderResult> GetValueAsync([NotNull] string key)
        {
            object value;
            ValueProviderResult result;
            if (_values.TryGetValue(key, out value))
            {
                var attemptedValue = value != null ? value.ToString() : null;
                result = new ValueProviderResult(value, attemptedValue, CultureInfo.InvariantCulture);
            }
            else
            {
                result = null;
            }
            
            return Task.FromResult(result);
        }
    }
}
