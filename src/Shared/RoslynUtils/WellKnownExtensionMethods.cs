// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.App.Analyzers.Infrastructure;

internal class WellKnownExtensionMethods
{
    private static readonly BoundedCacheWithFactory<Compilation, WellKnownExtensionMethods> LazyWellKnownExtensionMethodsCache = new();

    public static WellKnownExtensionMethods GetOrCreate(Compilation compilation) =>
        LazyWellKnownExtensionMethodsCache.GetOrCreateValue(compilation, static c => new WellKnownExtensionMethods(c));

    private readonly INamedTypeSymbol?[] _lazyWellKnownExtensionMethods;
    private readonly Compilation _compilation;

    static WellKnownExtensionMethods()
    {
        AssertEnumAndTableInSync();
    }

    [Conditional("DEBUG")]
    private static void AssertEnumAndTableInSync()
    {
        for (var i = 0; i < WellKnownTypeData.WellKnownTypeNames.Length; i++)
        {
            var name = WellKnownTypeData.WellKnownTypeNames[i];
            var typeId = (WellKnownTypeData.WellKnownType)i;

            var typeIdName = typeId.ToString().Replace("__", "+").Replace('_', '.');

            var separator = name.IndexOf('`');
            if (separator >= 0)
            {
                // Ignore type parameter qualifier for generic types.
                name = name.Substring(0, separator);
                typeIdName = typeIdName.Substring(0, separator);
            }

            Debug.Assert(name == typeIdName, $"Enum name ({typeIdName}) and type name ({name}) must match at {i}");
        }
    }

    private WellKnownExtensionMethods(Compilation compilation)
    {
        _lazyWellKnownExtensionMethods = new INamedTypeSymbol?[WellKnownTypeData.WellKnownTypeNames.Length];
        _compilation = compilation;
    }

    public INamedTypeSymbol Get(SpecialType type)
    {
        return _compilation.GetSpecialType(type);
    }

    public INamedTypeSymbol Get(WellKnownTypeData.WellKnownType type)
    {
        var index = (int)type;
        var symbol = _lazyWellKnownExtensionMethods[index];
        if (symbol is not null)
        {
            return symbol;
        }

        // Symbol hasn't been added to the cache yet.
        // Resolve symbol from name, cache, and return.
        return GetAndCache(index);
    }

    private INamedTypeSymbol GetAndCache(int index)
    {
        var result = _compilation.GetTypeByMetadataName(WellKnownTypeData.WellKnownTypeNames[index]);
        if (result == null)
        {
            throw new InvalidOperationException($"Failed to resolve well-known type '{WellKnownTypeData.WellKnownTypeNames[index]}'.");
        }
        Interlocked.CompareExchange(ref _lazyWellKnownExtensionMethods[index], result, null);

        // GetTypeByMetadataName should always return the same instance for a name.
        // To ensure we have a consistent value, for thread safety, return symbol set in the array.
        return _lazyWellKnownExtensionMethods[index]!;
    }

    public bool IsType(ITypeSymbol type, WellKnownTypeData.WellKnownType[] WellKnownExtensionMethods) => IsType(type, WellKnownExtensionMethods, out var _);

    public bool IsType(ITypeSymbol type, WellKnownTypeData.WellKnownType[] WellKnownExtensionMethods, [NotNullWhen(true)] out WellKnownTypeData.WellKnownType? match)
    {
        foreach (var wellKnownType in WellKnownExtensionMethods)
        {
            if (SymbolEqualityComparer.Default.Equals(type, Get(wellKnownType)))
            {
                match = wellKnownType;
                return true;
            }
        }

        match = null;
        return false;
    }

    public bool Implements(ITypeSymbol type, WellKnownTypeData.WellKnownType[] interfaceWellKnownExtensionMethods)
    {
        foreach (var wellKnownType in interfaceWellKnownExtensionMethods)
        {
            if (Implements(type, Get(wellKnownType)))
            {
                return true;
            }
        }

        return false;
    }

    public static bool Implements(ITypeSymbol? type, ITypeSymbol interfaceType)
    {
        if (type is null)
        {
            return false;
        }

        foreach (var t in type.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(t, interfaceType))
            {
                return true;
            }
        }
        return false;
    }
}
