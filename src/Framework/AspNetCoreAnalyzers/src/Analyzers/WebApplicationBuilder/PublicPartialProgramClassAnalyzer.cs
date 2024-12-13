// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PublicPartialProgramClassAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.PublicPartialProgramClassNotRequired);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(context =>
        {
            var syntaxNode = context.Node;
            if (IsPublicPartialClassProgram(syntaxNode))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.PublicPartialProgramClassNotRequired,
                    syntaxNode.GetLocation()));
            }
        }, SyntaxKind.ClassDeclaration);
    }

    private static bool IsPublicPartialClassProgram(SyntaxNode syntaxNode)
    {
        return syntaxNode is ClassDeclarationSyntax { Modifiers: { } modifiers } classDeclaration
            && classDeclaration.Parent is CompilationUnitSyntax parentNode
            && classDeclaration is { Identifier.ValueText: "Program" }
            && (classDeclaration.Members == null || classDeclaration.Members.Count == 0) // Skip non-empty declarations
            && modifiers is { Count: > 1 }
            && modifiers.Any(SyntaxKind.PublicKeyword)
            && modifiers.Any(SyntaxKind.PartialKeyword)
            && parentNode.DescendantNodes().Count() > 1;
    }
}
