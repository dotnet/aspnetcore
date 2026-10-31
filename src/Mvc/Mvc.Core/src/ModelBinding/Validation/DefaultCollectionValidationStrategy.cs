// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

/// <summary>
/// The default implementation of <see cref="IValidationStrategy"/> for a collection.
/// </summary>
/// <remarks>
/// This implementation handles cases like:
/// <example>
///     Model: IList&lt;Student&gt;
///     Query String: ?students[0].Age=8&amp;students[1].Age=9
///
///     In this case the elements of the collection are identified in the input data set by an incrementing
///     integer index.
/// </example>
///
/// or:
///
/// <example>
///     Model: IDictionary&lt;string, int&gt;
///     Query String: ?students[0].Key=Joey&amp;students[0].Value=8
///
///     In this case the dictionary is treated as a collection of key-value pairs, and the elements of the
///     collection are identified in the input data set by an incrementing integer index.
/// </example>
///
/// Using this key format, the enumerator enumerates model objects of type matching
/// <see cref="ModelMetadata.ElementMetadata"/>. The indices of the elements in the collection are used to
/// compute the model prefix keys.
/// </remarks>
internal sealed class DefaultCollectionValidationStrategy : IValidationStrategy
{
    private static readonly MethodInfo _getEnumerator = typeof(DefaultCollectionValidationStrategy)
        .GetMethod(nameof(GetEnumerator), BindingFlags.Static | BindingFlags.NonPublic)!;

    /// <summary>
    /// Gets an instance of <see cref="DefaultCollectionValidationStrategy"/>.
    /// </summary>
    public static readonly DefaultCollectionValidationStrategy Instance = new DefaultCollectionValidationStrategy();
    private readonly ConcurrentDictionary<Type, Func<object, IEnumerator>> _genericGetEnumeratorCache = new ConcurrentDictionary<Type, Func<object, IEnumerator>>();

    private DefaultCollectionValidationStrategy()
    {
    }

    /// <inheritdoc />
    public IEnumerator<ValidationEntry> GetChildren(
        ModelMetadata metadata,
        string key,
        object model)
    {
        var enumerator = GetEnumeratorForElementType(metadata, model);
        return new Enumerator(metadata.ElementMetadata!, key, enumerator);
    }

    public IEnumerator GetEnumeratorForElementType(ModelMetadata metadata, object model)
    {
        Func<object, IEnumerator> getEnumerator = _genericGetEnumeratorCache.GetOrAdd(
            key: metadata.ElementType!,
            valueFactory: (type) =>
            {
                var getEnumeratorMethod = _getEnumerator.MakeGenericMethod(type);
                var parameter = Expression.Parameter(typeof(object), "model");
                var expression =
                    Expression.Lambda<Func<object, IEnumerator>>(
                        Expression.Call(null, getEnumeratorMethod, parameter),
                        parameter);
                return expression.Compile();
            });

        return getEnumerator(model);
    }

    // Called via reflection.
    private static IEnumerator GetEnumerator<T>(object model)
    {
        return (model as IEnumerable<T>)?.GetEnumerator() ?? ((IEnumerable)model).GetEnumerator();
    }

    private sealed class Enumerator : IEnumerator<ValidationEntry>
    {
        private readonly string _key;
        private readonly ModelMetadata _metadata;
        private readonly IEnumerator _enumerator;

        private ValidationEntry _entry;
        private int _index;

        public Enumerator(
            ModelMetadata metadata,
            string key,
            IEnumerator enumerator)
        {
            _metadata = metadata;
            _key = key;
            _enumerator = enumerator;

            _index = -1;
        }

        public ValidationEntry Current => _entry;

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            if (!_enumerator.MoveNext())
            {
                return false;
            }

            var key = ModelNames.CreateIndexModelName(_key, _index);
            var model = _enumerator.Current;

            _entry = new ValidationEntry(_metadata, key, model);

            return true;
        }

        public void Dispose()
        {
        }

        public void Reset()
        {
            _enumerator.Reset();
        }
    }
}
