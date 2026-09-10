// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

/// <summary>
/// Analyzer that warns when code follows a <c>NavigationManager.NavigateTo</c> call in the same block.
/// In server-side (static SSR) contexts <c>NavigateTo</c> only signals the navigation; it no longer
/// stops execution, so the statements after it still run. Adding a <c>return</c> makes the intent explicit.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NavigateToReturnAnalyzer : DiagnosticAnalyzer
{
    private const string NavigationManagerTypeName = "Microsoft.AspNetCore.Components.NavigationManager";
    private const string NavigateToMethodName = "NavigateTo";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(DiagnosticDescriptors.CodeAfterNavigateToWillExecute);

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var navigationManagerType = compilationContext.Compilation.GetTypeByMetadataName(NavigationManagerTypeName);
            if (navigationManagerType is null)
            {
                // NavigationManager is not available in this compilation.
                return;
            }

            compilationContext.RegisterOperationAction(operationContext =>
            {
                var invocation = (IInvocationOperation)operationContext.Operation;
                if (invocation.TargetMethod.Name != NavigateToMethodName ||
                    !InheritsFromOrEquals(invocation.TargetMethod.ContainingType, navigationManagerType))
                {
                    return;
                }

                // Only expression statements can be followed by unreachable-looking code in the same block.
                if (invocation.Syntax is not InvocationExpressionSyntax invocationSyntax ||
                    invocationSyntax.Parent is not ExpressionStatementSyntax statement ||
                    statement.Parent is not BlockSyntax block)
                {
                    return;
                }

                var statements = block.Statements;
                var index = statements.IndexOf(statement);
                if (index < 0 || index == statements.Count - 1)
                {
                    // Last statement in the block: nothing runs after NavigateTo here.
                    return;
                }

                // A trailing return/throw is the recommended pattern, so it should not be flagged.
                if (statements[index + 1] is ReturnStatementSyntax or ThrowStatementSyntax)
                {
                    return;
                }

                operationContext.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.CodeAfterNavigateToWillExecute,
                    invocationSyntax.GetLocation()));
            }, OperationKind.Invocation);
        });
    }

    private static bool InheritsFromOrEquals(ITypeSymbol? type, INamedTypeSymbol baseType)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }
        }

        return false;
    }
}
