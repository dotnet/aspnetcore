// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Analyzers.AsyncVoidInMethodDeclaration;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class AsyncVoidInMethodDeclarationAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create<DiagnosticDescriptor>(new[]
        {
            DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration
        });

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationStartContext =>
        {
            if (!WellKnownTypes.TryCreate(compilationStartContext.Compilation, out var wellKnownTypes))
            {
                Debug.Fail("One or more types could not be found. This usually means you are bad at spelling C# type names.");
                return;
            }

            InitializeWorker(compilationStartContext, wellKnownTypes);
        });
    }

    private static void InitializeWorker(CompilationStartAnalysisContext compilationStartContext, WellKnownTypes wellKnownTypes)
    {
        compilationStartContext.RegisterSyntaxNodeAction(classContext =>
        {
            if (classContext.Node is not ClassDeclarationSyntax classDeclaration)
            {
                return;
            }

            var classSymbol = GetDeclaredSymbol<ITypeSymbol>(classContext, classDeclaration);

            if (IsController(classSymbol, wellKnownTypes) || IsSignalRHub(classSymbol, wellKnownTypes))
            {
                // scan all methods in class
                CheckMembers(classDeclaration.Members, wellKnownTypes, classContext, null);
            }
            else if (IsRazorPage(classSymbol, wellKnownTypes))
            {
                // only search for methods that follow a pattern: 'On + HttpMethodName'
                CheckMembers(classDeclaration.Members, wellKnownTypes, classContext, IsRazorPageHandlerMethod);
            }
            else if (IsMvcFilter(classSymbol, wellKnownTypes))
            {
                // only search for methods that follow a pattern: 'On + Filter
                CheckMembers(classDeclaration.Members, wellKnownTypes, classContext, IsMvcFilterMethod);
            }
        }, SyntaxKind.ClassDeclaration);
    }

    private static T? GetDeclaredSymbol<T>(SyntaxNodeAnalysisContext context, SyntaxNode syntax) where T : class
    {
        return context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) as T;
    }

    private static void CheckMembers(
        SyntaxList<MemberDeclarationSyntax> members,
        WellKnownTypes wellKnownTypes,
        SyntaxNodeAnalysisContext classContext,
        Func<IMethodSymbol?, WellKnownTypes, bool>? additionalMethodConstraint)
    {
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i] is not MethodDeclarationSyntax methodDeclarationSyntax)
            {
                continue;
            }

            var methodSymbol = GetDeclaredSymbol<IMethodSymbol>(classContext, methodDeclarationSyntax);
            // check if there is an additional filter for a method and the method pass through it
            if (additionalMethodConstraint != null && !additionalMethodConstraint(methodSymbol, wellKnownTypes))
            {
                continue;
            }

            if (ShouldFireDiagnostic(methodSymbol))
            {
                classContext.ReportDiagnostic(CreateDiagnostic(methodDeclarationSyntax));
            }
        }
    }

    private static bool ShouldFireDiagnostic(IMethodSymbol? methodSymbol)
    {
        return methodSymbol != null && methodSymbol.IsAsync && methodSymbol.ReturnsVoid;
    }

    private static Diagnostic CreateDiagnostic(SyntaxNode syntaxNode)
    {
        var location = Location.Create(syntaxNode.SyntaxTree, syntaxNode.FullSpan);
        return Diagnostic.Create(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, location);
    }
}
