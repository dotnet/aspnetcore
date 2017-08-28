// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Microsoft.AspNetCore.Mvc.Internal
{
    /// <summary>
    /// An implementation of <see cref="IValidationStrategy"/> for a dictionary bound with 'short form' style keys.
    /// </summary>
    /// <typeparam name="TKey">The <see cref="Type"/> of the keys of the model dictionary.</typeparam>
    /// <typeparam name="TValue">The <see cref="Type"/> of the values of the model dictionary.</typeparam>
    /// <remarks>
    /// This implementation handles cases like:
    /// <example>
    ///     Model: IDictionary&lt;string, Student&gt; 
    ///     Query String: ?students[Joey].Age=8&amp;students[Katherine].Age=9
    /// 
    ///     In this case, 'Joey' and 'Katherine' are the keys of the dictionary, used to bind two 'Student'
    ///     objects. The enumerator returned from this class will yield two 'Student' objects with corresponding
    ///     keys 'students[Joey]' and 'students[Katherine]'
    /// </example>
    /// 
    /// Using this key format, the enumerator enumerates model objects of type <typeparamref name="TValue"/>. The
    /// keys of the dictionary are not validated as they must be simple types.
    /// </remarks>
    public class ShortFormDictionaryValidationStrategy<TKey, TValue> : IValidationStrategy
    {
        private readonly ModelMetadata _valueMetadata;

        /// <summary>
        /// Creates a new <see cref="ShortFormDictionaryValidationStrategy{TKey, TValue}"/>.
        /// </summary>
        /// <param name="keyMappings">The mapping from model prefix key to dictionary key.</param>
        /// <param name="valueMetadata">
        /// The <see cref="ModelMetadata"/> associated with <typeparamref name="TValue"/>.
        /// </param>
        public ShortFormDictionaryValidationStrategy(
            IEnumerable<KeyValuePair<string, TKey>> keyMappings,
            ModelMetadata valueMetadata)
        {
            KeyMappings = keyMappings;
            _valueMetadata = valueMetadata;
        }

        /// <summary>
        /// Gets the mapping from model prefix key to dictionary key.
        /// </summary>
        public IEnumerable<KeyValuePair<string, TKey>> KeyMappings { get; }

        /// <inheritdoc />
        public IEnumerator<ValidationEntry> GetChildren(
            ModelMetadata metadata,
            string key,
            object model)
        {
            return new Enumerator(_valueMetadata, key, KeyMappings, (IDictionary<TKey, TValue>)model);
        }

        private class Enumerator : IEnumerator<ValidationEntry>
        {
            private readonly string _key;
            private readonly ModelMetadata _metadata;
            private readonly IDictionary<TKey, TValue> _model;
            private readonly IEnumerator<KeyValuePair<string, TKey>> _keyMappingEnumerator;

            private ValidationEntry _entry;

            public Enumerator(
                ModelMetadata metadata,
                string key,
                IEnumerable<KeyValuePair<string, TKey>> keyMappings,
                IDictionary<TKey, TValue> model)
            {
                _metadata = metadata;
                _key = key;
                _model = model;

                _keyMappingEnumerator = keyMappings.GetEnumerator();
            }

            public ValidationEntry Current => _entry;

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                TValue value;
                while (true)
                {
                    if (!_keyMappingEnumerator.MoveNext())
                    {
                        return false;
                    }

                    if (_model.TryGetValue(_keyMappingEnumerator.Current.Value, out value))
                    {
                        // Skip over entries that we can't find in the dictionary, they will show up as unvalidated.
                        break;
                    }
                }

                var key = ModelNames.CreateIndexModelName(_key, _keyMappingEnumerator.Current.Key);
                var model = value;

                _entry = new ValidationEntry(_metadata, key, model);

                return true;
            }

            public void Dispose()
            {
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
