// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class DictionaryBasedValueProvider<TBinderMetadata> : MetadataAwareValueProvider<TBinderMetadata>
        where TBinderMetadata : IValueProviderMetadata
    {
        private readonly IDictionary<string, object> _values;
        private PrefixContainer _prefixContainer;

        public DictionaryBasedValueProvider(IDictionary<string, object> values)
        {
            _values = values;
        }

        public override Task<bool> ContainsPrefixAsync(string key)
        {
            var prefixContainer = GetOrCreatePrefixContainer();
            return Task.FromResult(prefixContainer.ContainsPrefix(key));
        }

        private PrefixContainer GetOrCreatePrefixContainer()
        {
            if (_prefixContainer == null)
            {
                _prefixContainer = new PrefixContainer(_values.Keys);
            }

            return _prefixContainer;
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
