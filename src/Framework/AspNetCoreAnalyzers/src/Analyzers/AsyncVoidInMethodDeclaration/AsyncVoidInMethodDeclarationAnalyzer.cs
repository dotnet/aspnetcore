// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

            compilationStartContext.RegisterSyntaxNodeAction(classContext =>
            {
                if (classContext.Node is not ClassDeclarationSyntax classDeclaration)
                {
                    return;
                }

                if (IsController(classDeclaration, classContext, wellKnownTypes)
                    || IsActionFilter(classDeclaration, classContext, wellKnownTypes)
                    || IsSignalRHub(classDeclaration, classContext, wellKnownTypes))
                {
                    // scan for methods with async void signature
                    for (int i = 0; i < classDeclaration.Members.Count; i++)
                    {
                        if (classDeclaration.Members[i] is MethodDeclarationSyntax methodDeclarationSyntax)
                        {
                            if (ShouldFireDiagnostic(methodDeclarationSyntax))
                            {
                                classContext.ReportDiagnostic(CreateDiagnostic(classDeclaration));
                            }
                        }
                    }
                }
                else if (IsRazorPage(classDeclaration, classContext, wellKnownTypes))
                {
                    // search only for methods that follow a pattern: 'On + HttpMethodName'
                    for (int i = 0; i < classDeclaration.Members.Count; i++)
                    {
                        if (classDeclaration.Members[i] is MethodDeclarationSyntax methodDeclarationSyntax)
                        {
                            var methodSymbol = (IMethodSymbol?)classContext.SemanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                            if (IsRazorPageHandlerMethod(methodDeclarationSyntax, methodSymbol, wellKnownTypes))
                            {
                                if (ShouldFireDiagnostic(methodDeclarationSyntax))
                                {
                                    classContext.ReportDiagnostic(CreateDiagnostic(classDeclaration));
                                }
                            }
                        }
                    }
                }
            }, SyntaxKind.ClassDeclaration);
        });
    }

    private static bool ShouldFireDiagnostic(MethodDeclarationSyntax methodDeclarationSyntax)
    {
        // TODO: make method more nice
        var methodType = methodDeclarationSyntax.ReturnType;
        var methodModifier = methodDeclarationSyntax.Modifiers;

        return string.Equals(methodType.ToString(), "void") && string.Equals(methodModifier[1].ToString(), "async");
    }

    private static Diagnostic CreateDiagnostic(SyntaxNode syntaxNode)
    {
        var location = Location.Create(syntaxNode.SyntaxTree, syntaxNode.FullSpan);
        return Diagnostic.Create(DiagnosticDescriptors.AvoidAsyncVoidInMethodDeclaration, location);
    }
}
