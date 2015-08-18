// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IValueProvider"/> for form data stored in an <see cref="IDictionary{string, string[]}"/> and
    /// generally accessed asynchronously.
    /// </summary>
    public class JQueryFormValueProvider : BindingSourceValueProvider, IEnumerableValueProvider
    {
        private readonly Func<Task<IDictionary<string, string[]>>> _valuesFactory;

        private PrefixContainer _prefixContainer;
        private IDictionary<string, string[]> _values;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryBasedValueProvider"/> class.
        /// </summary>
        /// <param name="bindingSource">The <see cref="BindingSource"/> of the data.</param>
        /// <param name="valuesFactory">A delegate which provides the values to wrap.</param>
        /// <param name="culture">The culture to return with ValueProviderResult instances.</param>
        public JQueryFormValueProvider(
            [NotNull] BindingSource bindingSource,
            [NotNull] Func<Task<IDictionary<string, string[]>>> valuesFactory,
            CultureInfo culture)
            : base(bindingSource)
        {
            _valuesFactory = valuesFactory;
            Culture = culture;
        }

        // Internal for testing.
        internal JQueryFormValueProvider(
            [NotNull] BindingSource bindingSource,
            [NotNull] IDictionary<string, string[]> values,
            CultureInfo culture)
            : base(bindingSource)
        {
            _values = values;
            Culture = culture;
        }

        // Internal for testing
        internal CultureInfo Culture { get; }

        /// <inheritdoc />
        public override async Task<bool> ContainsPrefixAsync(string prefix)
        {
            var prefixContainer = await GetPrefixContainerAsync();
            return prefixContainer.ContainsPrefix(prefix);
        }

        /// <inheritdoc />
        public async Task<IDictionary<string, string>> GetKeysFromPrefixAsync(string prefix)
        {
            var prefixContainer = await GetPrefixContainerAsync();
            return prefixContainer.GetKeysFromPrefix(prefix);
        }

        /// <inheritdoc />
        public override async Task<ValueProviderResult> GetValueAsync(string key)
        {
            var dictionary = await GetDictionary();

            string[] values;
            if (dictionary.TryGetValue(key, out values) && values != null && values.Length > 0)
            {
                return new ValueProviderResult(values, Culture);
            }

            return ValueProviderResult.None;
        }

        private async Task<IDictionary<string, string[]>> GetDictionary()
        {
            if (_values == null)
            {
                Debug.Assert(_valuesFactory != null);

                // Initialization race is OK providing data remains read-only.
                _values = await _valuesFactory();
            }

            return _values;
        }

        private async Task<PrefixContainer> GetPrefixContainerAsync()
        {
            if (_prefixContainer == null)
            {
                var dictionary = await GetDictionary();

                // Initialization race is OK providing data remains read-only and object identity is not significant.
                _prefixContainer = new PrefixContainer(dictionary.Keys);
            }

            return _prefixContainer;
        }
    }
}
