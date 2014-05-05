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

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc.ModelBinding.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    public class ReadableStringCollectionValueProvider : IEnumerableValueProvider
    {
        private readonly CultureInfo _culture;
        private PrefixContainer _prefixContainer;
        private readonly IReadableStringCollection _values;

        /// <summary>
        /// Creates a NameValuePairsProvider wrapping an existing set of key value pairs.
        /// </summary>
        /// <param name="values">The key value pairs to wrap.</param>
        /// <param name="culture">The culture to return with ValueProviderResult instances.</param>
        public ReadableStringCollectionValueProvider([NotNull] IReadableStringCollection values, CultureInfo culture)
        {
            _values = values;
            _culture = culture;
        }

        public CultureInfo Culture
        {
            get
            {
                return _culture;
            }
        }

        private PrefixContainer PrefixContainer
        {
            get
            {
                if (_prefixContainer == null)
                {
                    // Initialization race is OK providing data remains read-only and object identity is not significant
                    // TODO: Figure out if we can have IReadableStringCollection expose Keys, Count etc

                    _prefixContainer = new PrefixContainer(_values.Select(v => v.Key).ToArray());
                }
                return _prefixContainer;
            }
        }

        public virtual Task<bool> ContainsPrefixAsync(string prefix)
        {
            return Task.FromResult(PrefixContainer.ContainsPrefix(prefix));
        }

        public virtual IDictionary<string, string> GetKeysFromPrefix([NotNull] string prefix)
        {
            return PrefixContainer.GetKeysFromPrefix(prefix);
        }

        public virtual Task<ValueProviderResult> GetValueAsync([NotNull] string key)
        {
            ValueProviderResult result;
            var values = _values.GetValues(key);
            if (values == null)
            {
                result = null;
            }
            else if (values.Count == 1)
            {
                var value = (string)values[0];
                result = new ValueProviderResult(value, value, _culture);
            }
            else
            {
                result = new ValueProviderResult(values, _values.Get(key), _culture);
            }

            return Task.FromResult(result);
        }
    }
}
