// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.App.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

using WellKnownType = WellKnownTypeData.WellKnownType;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public partial class RenderTreeBuilderAnalyzer : DiagnosticAnalyzer
{
    private const int SequenceParameterOrdinal = 0;
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            var wellKnownTypes = WellKnownTypes.GetOrCreate(compilation);

            context.RegisterOperationAction(context =>
            {
                var invocation = (IInvocationOperation)context.Operation;

                if (!IsRenderTreeBuilderMethodWithSequenceParameter(wellKnownTypes, invocation.TargetMethod))
                {
                    return;
                }

                foreach (var argument in invocation.Arguments)
                {
                    if (argument.Parameter?.Ordinal == SequenceParameterOrdinal)
                    {
                        if (!argument.Value.Syntax.IsKind(SyntaxKind.NumericLiteralExpression))
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers,
                                argument.Syntax.GetLocation(),
                                argument.Syntax.ToString()));
                        }

                        break;
                    }
                }

            }, OperationKind.Invocation);
        });
    }

    private static bool IsRenderTreeBuilderMethodWithSequenceParameter(WellKnownTypes wellKnownTypes, IMethodSymbol targetMethod)
        => SymbolEqualityComparer.Default.Equals(wellKnownTypes.Get(WellKnownType.Microsoft_AspNetCore_Components_Rendering_RenderTreeBuilder), targetMethod.ContainingType)
        && targetMethod.Parameters.Length > SequenceParameterOrdinal
        && targetMethod.Parameters[SequenceParameterOrdinal].Name == "sequence";
}
