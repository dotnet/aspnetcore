// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.ModelBinding
{
    /// <summary>
    /// A read-only list of <see cref="ModelMetadata"/> objects which represent model properties.
    /// </summary>
    public class ModelPropertyCollection : IReadOnlyList<ModelMetadata>
    {
        private readonly List<ModelMetadata> _properties;

        /// <summary>
        /// Creates a new <see cref="ModelPropertyCollection"/>.
        /// </summary>
        /// <param name="properties">The properties.</param>
        public ModelPropertyCollection([NotNull] IEnumerable<ModelMetadata> properties)
        {
            _properties = new List<ModelMetadata>(properties);
        }

        /// <inheritdoc />
        public ModelMetadata this[int index]
        {
            get
            {
                return _properties[index];
            }
        }

        /// <summary>
        /// Gets a <see cref="ModelMetadata"/> instance for the property corresponding to <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="propertyName">
        /// The property name. Property names are compared using <see cref="StringComparison.Ordinal"/>
        /// </param>
        /// <returns>
        /// The <see cref="ModelMetadata"/> instance for the property specified by <paramref name="propertyName"/>, or null
        /// if no match can be found.
        /// </returns>
        public ModelMetadata this[[NotNull] string propertyName]
        {

            get
            {
                foreach (var property in _properties)
                {
                    if (string.Equals(property.PropertyName, propertyName, StringComparison.Ordinal))
                    {
                        return property;
                    }
                }

                return null;
            }
        }

        /// <inheritdoc />
        public int Count
        {
            get
            {
                return _properties.Count;
            }
        }

        /// <inheritdoc />
        public IEnumerator<ModelMetadata> GetEnumerator()
        {
            return _properties.GetEnumerator();
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}