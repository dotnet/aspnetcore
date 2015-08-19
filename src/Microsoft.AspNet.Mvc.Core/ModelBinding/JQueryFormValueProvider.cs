// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IValueProvider"/> for form data stored in an <see cref="IDictionary{string, string[]}"/>.
    /// </summary>
    public class JQueryFormValueProvider : BindingSourceValueProvider, IEnumerableValueProvider
    {
        private PrefixContainer _prefixContainer;
        private readonly IDictionary<string, string[]> _values;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryBasedValueProvider"/> class.
        /// </summary>
        /// <param name="bindingSource">The <see cref="BindingSource"/> of the data.</param>
        /// <param name="valuesFactory">A delegate which provides the values to wrap.</param>
        /// <param name="culture">The culture to return with ValueProviderResult instances.</param>
        public JQueryFormValueProvider(
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
        public override bool ContainsPrefix(string prefix)
        {
            return PrefixContainer.ContainsPrefix(prefix);
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetKeysFromPrefix(string prefix)
        {
            return PrefixContainer.GetKeysFromPrefix(prefix);
        }

        /// <inheritdoc />
        public override ValueProviderResult GetValue(string key)
        {
            string[] values;
            if (_values.TryGetValue(key, out values) && values != null && values.Length > 0)
            {
                return new ValueProviderResult(values, Culture);
            }

            return ValueProviderResult.None;
        }
    }
}
