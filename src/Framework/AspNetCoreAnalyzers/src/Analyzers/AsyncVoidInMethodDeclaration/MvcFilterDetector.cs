// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public partial class AsyncVoidInMethodDeclarationAnalyzer
{
    private static bool IsActionFilter(ClassDeclarationSyntax classDeclaration, SyntaxNodeAnalysisContext context, WellKnownTypes wellKnownTypes) // TODO method unstable
    {
        // An ActionFilter class definition inherits from IActionFilter interface
        var classSymbol = (ITypeSymbol?)context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        // classSymbol?.BaseType || classSymbol?.Interfaces
        return ((classSymbol?.AllInterfaces.Contains(wellKnownTypes.IActionFilter) ?? false)
            || SymbolEqualityComparer.Default.Equals(wellKnownTypes.IActionFilter, classSymbol?.BaseType));
    }
}
