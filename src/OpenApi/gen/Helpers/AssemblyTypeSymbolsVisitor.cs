// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.OpenApi.SourceGenerators;

/// <summary>
/// Visits the assembly symbols to collect public types, properties, and methods that might
/// contain XML documentation comments.
/// </summary>
internal sealed class AssemblyTypeSymbolsVisitor(IAssemblySymbol assemblySymbol, CancellationToken cancellation) : SymbolVisitor
{
    private readonly CancellationToken _cancellationToken = cancellation;
    private readonly HashSet<INamedTypeSymbol> _exportedTypes = new(SymbolEqualityComparer.Default);
    private readonly HashSet<IPropertySymbol> _exportedProperties = new(SymbolEqualityComparer.Default);
    private readonly HashSet<IMethodSymbol> _exportedMethods = new(SymbolEqualityComparer.Default);

    public ImmutableArray<INamedTypeSymbol> GetPublicTypes() => [.. _exportedTypes];
    public ImmutableArray<IPropertySymbol> GetPublicProperties() => [.. _exportedProperties];
    public ImmutableArray<IMethodSymbol> GetPublicMethods() => [.. _exportedMethods];

    public void VisitAssembly() => VisitAssembly(assemblySymbol);

    public override void VisitAssembly(IAssemblySymbol symbol)
    {
        _cancellationToken.ThrowIfCancellationRequested();
        symbol.GlobalNamespace.Accept(this);
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var namespaceOrType in symbol.GetMembers())
        {
            _cancellationToken.ThrowIfCancellationRequested();
            namespaceOrType.Accept(this);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol type)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        if (!IsDeclaredInAssembly(type) || !_exportedTypes.Add(type))
        {
            return;
        }

        var nestedTypes = type.GetTypeMembers();

        foreach (var nestedType in nestedTypes)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            nestedType.Accept(this);
        }

        var properties = type.GetMembers().OfType<IPropertySymbol>();
        foreach (var property in properties)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (IsDeclaredInAssembly(property) && _exportedProperties.Add(property))
            {
                property.Type.Accept(this);
            }
        }
        var methods = type.GetMembers().OfType<IMethodSymbol>();
        foreach (var method in methods)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (IsDeclaredInAssembly(method) && _exportedMethods.Add(method))
            {
                method.Accept(this);
            }
        }
    }

    private bool IsDeclaredInAssembly(ISymbol symbol) =>
        SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, assemblySymbol);
}
