// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal static class IncrementalValuesProviderExtensions
{
    public static IncrementalValuesProvider<TSource> Distinct<TSource>(this IncrementalValuesProvider<TSource> source, IEqualityComparer<TSource> comparer)
    {
        return source
            .Collect()
            .WithComparer(ImmutableArrayEqualityComparer<TSource>.Instance)
            .SelectMany((values, cancellationToken) =>
            {
                if (values.IsEmpty)
                {
                    return values;
                }

                var results = ImmutableArray.CreateBuilder<TSource>(values.Length);
                HashSet<TSource> set = new(comparer);

                foreach (var value in values)
                {
                    if (set.Add(value))
                    {
                        results.Add(value);
                    }
                }

                return results.DrainToImmutable();
            });
    }

    public static IncrementalValuesProvider<T> Concat<T>(
        this IncrementalValuesProvider<ImmutableArray<T>> first,
        IncrementalValuesProvider<ImmutableArray<T>> second)
    {
        return first.Collect()
            .Combine(second.Collect())
            .SelectMany((tuple, _) =>
            {
                if (tuple.Left.IsEmpty && tuple.Right.IsEmpty)
                {
                    return [];
                }

                var results = ImmutableArray.CreateBuilder<T>(tuple.Left.Length + tuple.Right.Length);
                for (var i = 0; i < tuple.Left.Length; i++)
                {
                    results.AddRange(tuple.Left[i]);
                }
                for (var i = 0; i < tuple.Right.Length; i++)
                {
                    results.AddRange(tuple.Right[i]);
                }
                return results.DrainToImmutable();
            });
    }

    private sealed class ImmutableArrayEqualityComparer<T> : IEqualityComparer<ImmutableArray<T>>
    {
        public static readonly ImmutableArrayEqualityComparer<T> Instance = new();

        public bool Equals(ImmutableArray<T> x, ImmutableArray<T> y)
        {
            if (x.IsDefault)
            {
                return y.IsDefault;
            }
            else if (y.IsDefault)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(x[i], y[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public int GetHashCode(ImmutableArray<T> obj)
        {
            if (obj.IsDefault)
            {
                return 0;
            }
            var hashCode = -450793227;
            foreach (var item in obj)
            {
                hashCode = (hashCode * -1521134295) + EqualityComparer<T>.Default.GetHashCode(item);
            }

            return hashCode;
        }
    }
}
