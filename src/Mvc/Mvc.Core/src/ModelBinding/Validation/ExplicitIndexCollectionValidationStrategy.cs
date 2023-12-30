// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// An implementation of <see cref="IValidationStrategy"/> for a collection bound using 'explicit indexing'
/// style keys.
/// </summary>
/// <remarks>
/// This implementation handles cases like:
/// <example>
///     Model: IList&lt;Student&gt;
///     Query String: ?students.index=Joey,Katherine&amp;students[Joey].Age=8&amp;students[Katherine].Age=9
///
///     In this case, 'Joey' and 'Katherine' need to be used in the model prefix keys, but cannot be inferred
///     form inspecting the collection. These prefixes are captured during model binding, and mapped to
///     the corresponding ordinal index of a model object in the collection. The enumerator returned from this
///     class will yield two 'Student' objects with corresponding keys 'students[Joey]' and 'students[Katherine]'.
/// </example>
///
/// Using this key format, the enumerator enumerates model objects of type matching
/// <see cref="ModelMetadata.ElementMetadata"/>. The keys captured during model binding are mapped to the elements
/// in the collection to compute the model prefix keys.
/// </remarks>
internal sealed class ExplicitIndexCollectionValidationStrategy : IValidationStrategy
{
    /// <summary>
    /// Creates a new <see cref="ExplicitIndexCollectionValidationStrategy"/>.
    /// </summary>
    /// <param name="elementKeys">The keys of collection elements that were used during model binding.</param>
    public ExplicitIndexCollectionValidationStrategy(IEnumerable<string> elementKeys)
    {
        ArgumentNullException.ThrowIfNull(elementKeys);

        ElementKeys = elementKeys;
    }

    /// <summary>
    /// Gets the keys of collection elements that were used during model binding.
    /// </summary>
    public IEnumerable<string> ElementKeys { get; }

    /// <inheritdoc />
    public IEnumerator<ValidationEntry> GetChildren(
        ModelMetadata metadata,
        string key,
        object model)
    {
        var enumerator = DefaultCollectionValidationStrategy.Instance.GetEnumeratorForElementType(metadata, model);
        return new Enumerator(metadata.ElementMetadata!, key, ElementKeys, enumerator);
    }

    private sealed class Enumerator : IEnumerator<ValidationEntry>
    {
        private readonly string _key;
        private readonly ModelMetadata _metadata;
        private readonly IEnumerator _enumerator;
        private readonly IEnumerator<string> _keyEnumerator;

        private ValidationEntry _entry;

        public Enumerator(
            ModelMetadata metadata,
            string key,
            IEnumerable<string> elementKeys,
            IEnumerator enumerator)
        {
            _metadata = metadata;
            _key = key;

            _keyEnumerator = elementKeys.GetEnumerator();
            _enumerator = enumerator;
        }

        public ValidationEntry Current => _entry;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (!_keyEnumerator.MoveNext())
            {
                return false;
            }

            if (!_enumerator.MoveNext())
            {
                return false;
            }

            var model = _enumerator.Current;
            var key = ModelNames.CreateIndexModelName(_key, _keyEnumerator.Current);

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
