// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Mvc.ModelBinding
{
    /// <summary>
    /// An <see cref="IValueProvider"/> adapter for data stored in an <see cref="RouteValueDictionary"/>.
    /// </summary>
    public class RouteValueProvider : BindingSourceValueProvider
    {
        private readonly RouteValueDictionary _values;
        private PrefixContainer _prefixContainer;

        /// <summary>
        /// Creates a new <see cref="RouteValueProvider"/>.
        /// </summary>
        /// <param name="bindingSource">The <see cref="BindingSource"/> of the data.</param>
        /// <param name="values">The values.</param>
        /// <remarks>Sets <see cref="Culture"/> to <see cref="CultureInfo.InvariantCulture" />.</remarks>
        public RouteValueProvider(
            BindingSource bindingSource,
            RouteValueDictionary values)
            : this(bindingSource, values, CultureInfo.InvariantCulture)
        {
        }

        /// <summary>
        /// Creates a new <see cref="RouteValueProvider"/>.
        /// </summary>
        /// <param name="bindingSource">The <see cref="BindingSource"/> of the data.</param>
        /// <param name="values">The values.</param>
        /// <param name="culture">The culture for route value.</param>
        public RouteValueProvider(BindingSource bindingSource, RouteValueDictionary values, CultureInfo culture)
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

            if (culture == null)
            {
                throw new ArgumentNullException(nameof(culture));
            }

            _values = values;
            Culture = culture;
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

        protected CultureInfo Culture { get; }

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

            if (key.Length == 0)
            {
                // Top level parameters will fall back to an empty prefix when the parameter name does not
                // appear in any value provider. This would result in the parameter binding to a route value
                // an empty key which isn't a scenario we want to support.
                // Return a "None" result in this event.
                return ValueProviderResult.None;
            }

            if (_values.TryGetValue(key, out var value))
            {
                var stringValue = value as string ?? Convert.ToString(value, Culture) ?? string.Empty;
                return new ValueProviderResult(stringValue, Culture);
            }
            else
            {
                return ValueProviderResult.None;
            }
        }
    }
}
