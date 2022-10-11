// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers.IHeaderDictionary;

internal sealed class IHeaderDictionarySymbols
{
    public IHeaderDictionarySymbols(Compilation compilation)
    {
        IHeaderDictionary = compilation.GetTypeByMetadataName(SymbolNames.IHeaderDictionary.MetadataName);
    }

    public bool HasRequiredSymbols => IHeaderDictionary != null;

    public INamedTypeSymbol IHeaderDictionary { get; }
}
