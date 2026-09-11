// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

#nullable enable

namespace Microsoft.AspNetCore.Components.Analyzers;

/// <summary>
/// Analyzer that warns when code follows a <c>NavigationManager.NavigateTo</c> call in the same statement list.
/// In server-side (static SSR) contexts <c>NavigateTo</c> only signals the navigation; it no longer
/// stops execution, so the statements after it still run. Adding a <c>return</c> makes the intent explicit.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NavigateToReturnAnalyzer : DiagnosticAnalyzer
{
    private const string NavigationManagerTypeName = "Microsoft.AspNetCore.Components.NavigationManager";
    private const string NavigateToMethodName = "NavigateTo";
    private const string DisableThrowNavigationExceptionProperty = "build_property.BlazorDisableThrowNavigationException";

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

                var analyzerOptions = operationContext.Options.AnalyzerConfigOptionsProvider.GetOptions(invocation.Syntax.SyntaxTree);
                if (!analyzerOptions.TryGetValue(DisableThrowNavigationExceptionProperty, out var disableThrowNavigationException) ||
                    !string.Equals(disableThrowNavigationException, bool.TrueString, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Only expression statements can be followed by unreachable-looking code in the same statement list.
                if (invocation.Syntax is not InvocationExpressionSyntax invocationSyntax ||
                    invocationSyntax.Parent is not ExpressionStatementSyntax statement ||
                    !TryGetContainingStatements(statement, out var statements))
                {
                    return;
                }

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

    private static bool TryGetContainingStatements(ExpressionStatementSyntax statement, out SyntaxList<StatementSyntax> statements)
    {
        switch (statement.Parent)
        {
            case BlockSyntax block:
                statements = block.Statements;
                return true;
            case SwitchSectionSyntax switchSection:
                statements = switchSection.Statements;
                return true;
            default:
                statements = default;
                return false;
        }
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
