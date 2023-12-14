// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

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
internal sealed class ShortFormDictionaryValidationStrategy<TKey, TValue> : IValidationStrategy
    where TKey : notnull
{
    private readonly ModelMetadata _valueMetadata;

    /// <summary>
    /// Creates a new <see cref="ShortFormDictionaryValidationStrategy{TKey, TValue}"/>.
    /// </summary>
    /// <param name="keyMappings">
    /// The mapping from <see cref="ModelStateDictionary"/> key to dictionary key.
    /// </param>
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
    /// Gets the mapping from <see cref="ModelStateDictionary"/> key to dictionary key.
    /// </summary>
    public IEnumerable<KeyValuePair<string, TKey>> KeyMappings { get; }

    /// <inheritdoc />
    public IEnumerator<ValidationEntry> GetChildren(
        ModelMetadata metadata,
        string key,
        object model)
    {
        // key is not needed because KeyMappings maps from full ModelState keys to dictionary keys.
        return new Enumerator(_valueMetadata, KeyMappings, (IDictionary<TKey, TValue>)model);
    }

    private sealed class Enumerator : IEnumerator<ValidationEntry>
    {
        private readonly ModelMetadata _metadata;
        private readonly IDictionary<TKey, TValue> _model;
        private readonly IEnumerator<KeyValuePair<string, TKey>> _keyMappingEnumerator;

        private ValidationEntry _entry;

        public Enumerator(
            ModelMetadata metadata,
            IEnumerable<KeyValuePair<string, TKey>> keyMappings,
            IDictionary<TKey, TValue> model)
        {
            _metadata = metadata;
            _model = model;
            _keyMappingEnumerator = keyMappings.GetEnumerator();
        }

        public ValidationEntry Current => _entry;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            TValue? value;
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

            _entry = new ValidationEntry(_metadata, _keyMappingEnumerator.Current.Key, value);

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
