// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.Internal;

#if JSONNET
namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;
#else
using Microsoft.AspNetCore.Mvc.Core;
namespace Microsoft.AspNetCore.Mvc.Infrastructure;
#endif

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
        BindingFlags.NonPublic | BindingFlags.Instance)!;

    private readonly ConcurrentDictionary<Type, Func<object, CancellationToken, Task<ICollection>>?> _asyncEnumerableConverters = new();
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
    /// Attempts to produces a delegate that reads an <see cref="IAsyncEnumerable{T}"/> into an <see cref="ICollection{T}"/>.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <param name="reader">A delegate that when awaited reads the <see cref="IAsyncEnumerable{T}"/>.</param>
    /// <returns><see langword="true" /> when <paramref name="type"/> is an instance of <see cref="IAsyncEnumerable{T}"/>, othwerise <see langword="false"/>.</returns>
    public bool TryGetReader(Type type, [NotNullWhen(true)] out Func<object, CancellationToken, Task<ICollection>>? reader)
    {
        if (!_asyncEnumerableConverters.TryGetValue(type, out reader))
        {
            var enumerableType = ClosedGenericMatcher.ExtractGenericInterface(type, typeof(IAsyncEnumerable<>));
            if (enumerableType is null)
            {
                // Not an IAsyncEnumerable<T>. Cache this result so we avoid reflection the next time we see this type.
                reader = null;
                _asyncEnumerableConverters.TryAdd(type, reader);
            }
            else
            {
                var enumeratedObjectType = enumerableType.GetGenericArguments()[0];

                var converter = (Func<object, CancellationToken, Task<ICollection>>)Converter
                    .MakeGenericMethod(enumeratedObjectType)
                    .CreateDelegate(typeof(Func<object, CancellationToken, Task<ICollection>>), this);

                reader = converter;
                _asyncEnumerableConverters.TryAdd(type, reader);
            }
        }

        return reader != null;
    }

    private async Task<ICollection> ReadInternal<T>(object value, CancellationToken cancellationToken)
    {
        var asyncEnumerable = ((IAsyncEnumerable<T>)value).WithCancellation(cancellationToken);
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
