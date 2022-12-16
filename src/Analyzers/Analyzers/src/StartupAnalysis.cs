// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class StartupAnalysis
{
    private readonly ImmutableDictionary<INamedTypeSymbol, ImmutableArray<object>> _analysesByType;

    public StartupAnalysis(
        StartupSymbols startupSymbols,
        ImmutableDictionary<INamedTypeSymbol, ImmutableArray<object>> analysesByType)
    {
        StartupSymbols = startupSymbols;
        _analysesByType = analysesByType;
    }

    public StartupSymbols StartupSymbols { get; }

    public T? GetRelatedSingletonAnalysis<T>(INamedTypeSymbol type) where T : class
    {
        if (_analysesByType.TryGetValue(type, out var list))
        {
            for (var i = 0; i < list.Length; i++)
            {
                if (list[i] is T item)
                {
                    return item;
                }
            }
        }

        return null;
    }

    public ImmutableArray<T> GetRelatedAnalyses<T>(INamedTypeSymbol type) where T : class
    {
        var items = ImmutableArray.CreateBuilder<T>();
        if (_analysesByType.TryGetValue(type, out var list))
        {
            for (var i = 0; i < list.Length; i++)
            {
                if (list[i] is T item)
                {
                    items.Add(item);
                }
            }
        }

        return items.ToImmutable();
    }
}
