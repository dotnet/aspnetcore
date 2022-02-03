// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class RenderTreeBuilderAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(new[]
    {
        DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers,
    });

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationStartAnalysisContext =>
        {
            var compilation = compilationStartAnalysisContext.Compilation;

            if (!WellKnownTypes.TryCreate(compilation, out var wellKnownTypes))
            {
                return;
            }

            compilationStartAnalysisContext.RegisterOperationAction(operationAnalysisContext =>
            {
                var invocation = (IInvocationOperation)operationAnalysisContext.Operation;

                if (!IsRenderTreeBuilderMethodWithSequenceParameter(wellKnownTypes, invocation.TargetMethod))
                {
                    return;
                }

                var sequenceArgument = invocation.Arguments[0];

                if (!sequenceArgument.Value.Syntax.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    operationAnalysisContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers,
                        sequenceArgument.Syntax.GetLocation(),
                        sequenceArgument.Syntax.ToString()));
                }
            }, OperationKind.Invocation);
        });
    }

    private static bool IsRenderTreeBuilderMethodWithSequenceParameter(WellKnownTypes wellKnownTypes, IMethodSymbol targetMethod)
        => SymbolEqualityComparer.Default.Equals(wellKnownTypes.RenderTreeBuilder, targetMethod.ContainingType)
        && targetMethod.Parameters.Length != 0
        && targetMethod.Parameters[0].Name == "sequence";
}
