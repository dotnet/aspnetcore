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
                // scan for methods with async void signature
                for (int i = 0; i < classDeclaration.Members.Count; i++)
                {
                    if (classDeclaration.Members[i] is not MethodDeclarationSyntax methodDeclarationSyntax)
                    {
                        continue;
                    }

                    if (ShouldFireDiagnostic(methodDeclarationSyntax))
                    {
                        classContext.ReportDiagnostic(CreateDiagnostic(classDeclaration));
                    }
                }
            }
            else if (IsRazorPage(classSymbol, wellKnownTypes) || IsMvcFilter(classSymbol, wellKnownTypes))
            {
                // search only for methods that follow a pattern: 'On + HttpMethodName'
                for (int i = 0; i < classDeclaration.Members.Count; i++)
                {
                    if (classDeclaration.Members[i] is not MethodDeclarationSyntax methodDeclarationSyntax)
                    {
                        continue;
                    }

                    var methodSymbol = GetDeclaredSymbol<IMethodSymbol>(classContext,methodDeclarationSyntax);
                    if (IsRazorPageHandlerMethod(methodSymbol, wellKnownTypes) || IsFilterMethod())
                    {
                        if (ShouldFireDiagnostic(methodDeclarationSyntax))
                        {
                            classContext.ReportDiagnostic(CreateDiagnostic(classDeclaration));
                        }
                    }
                }
            }
        }, SyntaxKind.ClassDeclaration);
    }

    private static T? GetDeclaredSymbol<T>(SyntaxNodeAnalysisContext context, SyntaxNode syntax) where T : class
    {
        return context.SemanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) as T;
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
