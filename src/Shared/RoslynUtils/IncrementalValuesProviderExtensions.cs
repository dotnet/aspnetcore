// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

internal static class IncrementalValuesProviderExtensions
{
    public static IncrementalValuesProvider<(TSource Source, ImmutableArray<TElement> Elements)> GroupWith<TSource, TElement>(
        this IncrementalValuesProvider<TSource> source,
        Func<TSource, TElement> sourceToElementTransform,
        IEqualityComparer<TSource> comparer)
    {
        return source.Collect().SelectMany((values, _) =>
        {
            Dictionary<TSource, ImmutableArray<TElement>.Builder> map = new(comparer);
            foreach (var value in values)
            {
                if (!map.TryGetValue(value, out ImmutableArray<TElement>.Builder builder))
                {
                    builder = ImmutableArray.CreateBuilder<TElement>();
                    map.Add(value, builder);
                }
                builder.Add(sourceToElementTransform(value));
            }
            ImmutableArray<(TSource Key, ImmutableArray<TElement> Elements)>.Builder result =
                ImmutableArray.CreateBuilder<(TSource, ImmutableArray<TElement>)>();
            foreach (KeyValuePair<TSource, ImmutableArray<TElement>.Builder> entry in map)
            {
                result.Add((entry.Key, entry.Value.ToImmutable()));
            }
            return result;
        });
    }
}
