// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Analyzers.RenderTreeBuilder;

public partial class RenderTreeBuilderAnalyzer : DiagnosticAnalyzer
{
    private static void DisallowNonLiteralSequenceNumbers(
        in OperationAnalysisContext context,
        IArgumentOperation sequenceArgument)
    {
        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DoNotUseNonLiteralSequenceNumbers,
            sequenceArgument.Syntax.GetLocation(),
            sequenceArgument.Syntax.ToString()));
    }
}
