// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Core;
using Microsoft.Extensions.Internal;

#if JSONNET
namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson
#else
namespace Microsoft.AspNetCore.Mvc.Infrastructure
#endif
{
    using ReaderFunc = Func<IAsyncEnumerable<object>, Task<ICollection>>;

    /// <summary>
    /// Type that reads an <see cref="IAsyncEnumerable{T}"/> instance into a
    /// generic collection instance.
    /// </summary>
    /// <remarks>
    /// This type is used to create a strongly typed synchronous <see cref="ICollection{T}"/> instance from
    /// an <see cref="IAsyncEnumerable{T}"/>. An accurate <see cref="ICollection{T}"/> is required for XML formatters to
    /// correctly serialize.
    /// </remarks>
    internal sealed class AsyncEnumerableReader
    {
        private readonly MethodInfo Converter = typeof(AsyncEnumerableReader).GetMethod(
            nameof(ReadInternal),
            BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly ConcurrentDictionary<Type, ReaderFunc> _asyncEnumerableConverters =
            new ConcurrentDictionary<Type, ReaderFunc>();
        private readonly MvcOptions _mvcOptions;

        /// <summary>
        /// Initializes a new instance of <see cref="AsyncEnumerableReader"/>.
        /// </summary>
        /// <param name="mvcOptions">Accessor to <see cref="MvcOptions"/>.</param>
        public AsyncEnumerableReader(MvcOptions mvcOptions)
        {
            _mvcOptions = mvcOptions;
        }

        /// <summary>
        /// Reads a <see cref="IAsyncEnumerable{T}"/> into an <see cref="ICollection{T}"/>.
        /// </summary>
        /// <param name="value">The <see cref="IAsyncEnumerable{T}"/> to read.</param>
        /// <returns>The <see cref="ICollection"/>.</returns>
        public Task<ICollection> ReadAsync(IAsyncEnumerable<object> value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var type = value.GetType();
            if (!_asyncEnumerableConverters.TryGetValue(type, out var result))
            {
                var enumerableType = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IAsyncEnumerable<>));
                Debug.Assert(enumerableType != null);

                var enumeratedObjectType = enumerableType.GetGenericArguments()[0];

                var converter = (ReaderFunc)Converter
                    .MakeGenericMethod(enumeratedObjectType)
                    .CreateDelegate(typeof(ReaderFunc), this);

                _asyncEnumerableConverters.TryAdd(type, converter);
                result = converter;
            }

            return result(value);
        }

        private async Task<ICollection> ReadInternal<T>(IAsyncEnumerable<object> value)
        {
            var asyncEnumerable = (IAsyncEnumerable<T>)value;
            var result = new List<T>();
            var count = 0;

            await foreach (var item in asyncEnumerable)
            {
                if (count++ >= _mvcOptions.MaxIAsyncEnumerableBufferLimit)
                {
                    throw new InvalidOperationException(Resources.FormatObjectResultExecutor_MaxEnumerationExceeded(
                        nameof(AsyncEnumerableReader),
                        value.GetType()));
                }

                result.Add(item);
            }

            return result;
        }
    }
}
