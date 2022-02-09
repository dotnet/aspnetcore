// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

public partial class AsyncVoidInMethodDeclarationAnalyzer
{
    private static bool IsSignalRHub(ClassDeclarationSyntax classDeclaration, SyntaxNodeAnalysisContext context, WellKnownTypes wellKnownTypes)
    {
        // Since Hub or Hub<T> are abstract classes they will be placed onto BaseType position
        // (otherwise the csharp(CS1722) rule will be triggered resulting in "Base class 'Hub' must come before any interfaces" error)
        // so that there is no need to check Interfaces property of the classSymbol

        INamedTypeSymbol lookupType = wellKnownTypes.SignalRHub;
        var classSymbol = (ITypeSymbol?)context.SemanticModel.GetDeclaredSymbol(classDeclaration);

        return classSymbol?.BaseType != null && lookupType.IsAssignableFrom(classSymbol?.BaseType!);
    }
}
