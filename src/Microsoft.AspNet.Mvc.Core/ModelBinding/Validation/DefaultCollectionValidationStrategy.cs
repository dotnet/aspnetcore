// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Microsoft.AspNet.Mvc.ModelBinding.Validation
{
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
    public class DefaultCollectionValidationStrategy : IValidationStrategy
    {
        private static readonly MethodInfo _getEnumerator = typeof(DefaultCollectionValidationStrategy)
            .GetMethod(nameof(GetEnumerator), BindingFlags.Static | BindingFlags.NonPublic);

        /// <summary>
        /// Gets an instance of <see cref="DefaultCollectionValidationStrategy"/>.
        /// </summary>
        public static readonly IValidationStrategy Instance = new DefaultCollectionValidationStrategy();

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
            return new Enumerator(metadata.ElementMetadata, key, enumerator);
        }

        public static IEnumerator GetEnumeratorForElementType(ModelMetadata metadata, object model)
        {
            var getEnumeratorMethod = _getEnumerator.MakeGenericMethod(metadata.ElementType);
            return (IEnumerator)getEnumeratorMethod.Invoke(null, new object[] { model });
        }

        // Called via reflection.
        private static IEnumerator GetEnumerator<T>(object model)
        {
            return (model as IEnumerable<T>)?.GetEnumerator() ?? ((IEnumerable)model).GetEnumerator();
        }

        private class Enumerator : IEnumerator<ValidationEntry>
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

            public ValidationEntry Current
            {
                get
                {
                    return _entry;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return Current;
                }
            }

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
}
