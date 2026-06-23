// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Components.Testing.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal class ServiceOverrideAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(
            DiagnosticDescriptors.MethodNotFound,
            DiagnosticDescriptors.NonConstantMethodName,
            DiagnosticDescriptors.MethodMustBeStatic);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        // Fast path: check if the method name looks like ConfigureServices
        if (GetInvokedMethodName(invocation) != "ConfigureServices")
        {
            return;
        }

        var semanticModel = context.SemanticModel;
        var symbolInfo = semanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol method)
        {
            return;
        }

        // Only analyze ServerStartOptions.ConfigureServices calls
        if (method.Name != "ConfigureServices" ||
            method.ContainingType?.ToDisplayString() !=
                "Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions")
        {
            return;
        }

        // Skip calls within ServerStartOptions itself (generic → non-generic forwarding)
        var containingSymbol = semanticModel.GetEnclosingSymbol(
            invocation.SpanStart, context.CancellationToken);
        if (containingSymbol?.ContainingType?.ToDisplayString() ==
            "Microsoft.AspNetCore.Components.Testing.Infrastructure.ServerStartOptions")
        {
            return;
        }

        ITypeSymbol? overrideType = null;
        ExpressionSyntax? methodNameExpr = null;

        if (method.IsGenericMethod && method.TypeArguments.Length == 1)
        {
            overrideType = method.TypeArguments[0];
            if (invocation.ArgumentList.Arguments.Count >= 1)
            {
                methodNameExpr = invocation.ArgumentList.Arguments[0].Expression;
            }
        }
        else if (!method.IsGenericMethod && method.Parameters.Length == 2)
        {
            if (invocation.ArgumentList.Arguments.Count >= 2)
            {
                var typeArg = invocation.ArgumentList.Arguments[0].Expression;
                if (typeArg is TypeOfExpressionSyntax typeOfExpr)
                {
                    var typeInfo = semanticModel.GetTypeInfo(
                        typeOfExpr.Type, context.CancellationToken);
                    overrideType = typeInfo.Type;
                }

                methodNameExpr = invocation.ArgumentList.Arguments[1].Expression;
            }
        }

        if (overrideType is null || methodNameExpr is null)
        {
            return;
        }

        // Resolve the method name constant
        var constantValue = semanticModel.GetConstantValue(
            methodNameExpr, context.CancellationToken);
        if (!constantValue.HasValue || constantValue.Value is not string methodName)
        {
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.NonConstantMethodName,
                    methodNameExpr.GetLocation()));
            return;
        }

        // Look for ANY method with the given name and IServiceCollection parameter
        var candidates = overrideType.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m =>
                m.Parameters.Length == 1 &&
                m.Parameters[0].Type.ToDisplayString() ==
                    "Microsoft.Extensions.DependencyInjection.IServiceCollection")
            .ToList();

        if (candidates.Count == 0)
        {
            // Method not found at all
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.MethodNotFound,
                    invocation.GetLocation(),
                    methodName,
                    overrideType.ToDisplayString()));
            return;
        }

        // Method found — check if it's static
        var targetMethod = candidates.FirstOrDefault(m => m.IsStatic);
        if (targetMethod is null)
        {
            // Method exists but is not static
            context.ReportDiagnostic(
                Diagnostic.Create(
                    DiagnosticDescriptors.MethodMustBeStatic,
                    invocation.GetLocation(),
                    methodName,
                    overrideType.ToDisplayString()));
        }
    }

    static string GetInvokedMethodName(InvocationExpressionSyntax invocation)
    {
        switch (invocation.Expression)
        {
            case MemberAccessExpressionSyntax memberAccess:
                return memberAccess.Name switch
                {
                    GenericNameSyntax generic => generic.Identifier.Text,
                    IdentifierNameSyntax identifier => identifier.Identifier.Text,
                    _ => ""
                };
            case GenericNameSyntax generic:
                return generic.Identifier.Text;
            case IdentifierNameSyntax identifier:
                return identifier.Identifier.Text;
            default:
                return "";
        }
    }
}
