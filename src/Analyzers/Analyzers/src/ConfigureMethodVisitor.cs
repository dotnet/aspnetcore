// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Microsoft.AspNetCore.Analyzers;

internal sealed class ConfigureMethodVisitor : SymbolVisitor
{
    public static List<IMethodSymbol> FindConfigureMethods(StartupSymbols symbols, IAssemblySymbol assembly)
    {
        var visitor = new ConfigureMethodVisitor(symbols);
        visitor.Visit(assembly);
        return visitor._methods;
    }

    private readonly StartupSymbols _symbols;
    private readonly List<IMethodSymbol> _methods;

    private ConfigureMethodVisitor(StartupSymbols symbols)
    {
        _symbols = symbols;
        _methods = new List<IMethodSymbol>();
    }

    public override void VisitAssembly(IAssemblySymbol symbol)
    {
        Visit(symbol.GlobalNamespace);
    }

    public override void VisitNamespace(INamespaceSymbol symbol)
    {
        foreach (var type in symbol.GetTypeMembers())
        {
            Visit(type);
        }

        foreach (var @namespace in symbol.GetNamespaceMembers())
        {
            Visit(@namespace);
        }
    }

    public override void VisitNamedType(INamedTypeSymbol symbol)
    {
        foreach (var member in symbol.GetMembers())
        {
            Visit(member);
        }
    }

    public override void VisitMethod(IMethodSymbol symbol)
    {
        if (StartupFacts.IsConfigure(_symbols, symbol))
        {
            _methods.Add(symbol);
        }
    }
}
