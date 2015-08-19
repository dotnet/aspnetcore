// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Http;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IValueProvider"/> adapter for data stored in an <see cref="IReadableStringCollection"/>.
    /// </summary>
    public class ReadableStringCollectionValueProvider : BindingSourceValueProvider, IEnumerableValueProvider
    {
        private readonly CultureInfo _culture;
        private PrefixContainer _prefixContainer;
        private IReadableStringCollection _values;

        /// <summary>
        /// Creates a provider for <see cref="IReadableStringCollection"/> wrapping an existing set of key value pairs.
        /// </summary>
        /// <param name="bindingSource">The <see cref="BindingSource"/> for the data.</param>
        /// <param name="values">The key value pairs to wrap.</param>
        /// <param name="culture">The culture to return with ValueProviderResult instances.</param>
        public ReadableStringCollectionValueProvider(
            [NotNull] BindingSource bindingSource,
            [NotNull] IReadableStringCollection values, 
            CultureInfo culture)
            : base(bindingSource)
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
        public virtual IDictionary<string, string> GetKeysFromPrefix([NotNull] string prefix)
        {
            return PrefixContainer.GetKeysFromPrefix(prefix);
        }

        /// <inheritdoc />
        public override ValueProviderResult GetValue([NotNull] string key)
        {
            var values = _values.GetValues(key);
            if (values == null)
            {
                return ValueProviderResult.None;
            }
            else
            {
                return new ValueProviderResult(values.ToArray(), _culture);
            }
        }
    }
}
