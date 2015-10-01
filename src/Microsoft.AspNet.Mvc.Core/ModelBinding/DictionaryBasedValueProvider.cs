// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IValueProvider"/> adapter for data stored in an
    /// <see cref="IDictionary{string, object}"/>.
    /// </summary>
    public class DictionaryBasedValueProvider : BindingSourceValueProvider
    {
        private readonly IDictionary<string, object> _values;
        private PrefixContainer _prefixContainer;

        /// <summary>
        /// Creates a new <see cref="DictionaryBasedValueProvider"/>.
        /// </summary>
        /// <param name="bindingSource">The <see cref="BindingSource"/> of the data.</param>
        /// <param name="values">The values.</param>
        public DictionaryBasedValueProvider(
            BindingSource bindingSource,
            IDictionary<string, object> values)
            : base(bindingSource)
        {
            if (bindingSource == null)
            {
                throw new ArgumentNullException(nameof(bindingSource));
            }

            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _values = values;
        }

        protected PrefixContainer PrefixContainer
        {
            get
            {
                if (_prefixContainer == null)
                {
                    _prefixContainer = new PrefixContainer(_values.Keys);
                }

                return _prefixContainer;
            }
        }

        /// <inheritdoc />
        public override bool ContainsPrefix(string key)
        {
            return PrefixContainer.ContainsPrefix(key);
        }

        /// <inheritdoc />
        public override ValueProviderResult GetValue(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            object value;
            if (_values.TryGetValue(key, out value))
            {
                var stringValue = value as string ?? value?.ToString() ?? string.Empty;
                return new ValueProviderResult(stringValue, CultureInfo.InvariantCulture);
            }
            else
            {
                return ValueProviderResult.None;
            }
        }
    }
}
