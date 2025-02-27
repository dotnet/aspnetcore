// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Http.ValidationsGenerator;

internal static class CompilationExtensions
{
    public static IEnumerable<INamedTypeSymbol> GetDelegatesWithAttribute(this Compilation compilation, string name)
    {
        var delegates = new List<INamedTypeSymbol>();

        // Inspect the current compilation
        delegates.AddRange(compilation.SyntaxTrees
            .SelectMany(syntaxTree => syntaxTree.GetRoot().DescendantNodes())
            .OfType<VariableDeclaratorSyntax>()
            .Select(variableSyntax => compilation.GetSemanticModel(variableSyntax.SyntaxTree).GetDeclaredSymbol(variableSyntax))
            .OfType<INamedTypeSymbol>()
            .Where(delegateSymbol => delegateSymbol.GetAttributes().Any(attr => attr.AttributeClass!.Name.Equals(name, System.StringComparison.Ordinal))));

        // Inspect referenced assemblies
        foreach (var reference in compilation.References)
        {
            if (compilation.GetAssemblyOrModuleSymbol(reference) is not IAssemblySymbol symbol)
            {
                continue;
            }

            foreach (var type in symbol.GlobalNamespace.GetNamespaceMembers().SelectMany(ns => ns.GetTypeMembers()))
            {
                foreach (var member in type.GetMembers().OfType<IFieldSymbol>())
                {
                    if (member.GetAttributes().Any(attr => attr.AttributeClass!.Name.Equals(name, System.StringComparison.Ordinal)))
                    {
                        delegates.Add(type);
                    }
                }
            }
        }

        return delegates;
    }
}
