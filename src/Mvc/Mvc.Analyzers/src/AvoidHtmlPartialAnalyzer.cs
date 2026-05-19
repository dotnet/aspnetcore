// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.AspNetCore.Mvc.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AvoidHtmlPartialAnalyzer : ViewFeatureAnalyzerBase
{
    public AvoidHtmlPartialAnalyzer()
        : base(DiagnosticDescriptors.MVC1000_HtmlHelperPartialShouldBeAvoided)
    {
    }

    protected override void InitializeWorker(ViewFeaturesAnalyzerContext analyzerContext)
    {
        analyzerContext.Context.RegisterOperationAction(context =>
        {
            var method = ((IInvocationOperation)context.Operation).TargetMethod;
            if (!analyzerContext.IsHtmlHelperExtensionMethod(method))
            {
                return;
            }

            if (string.Equals(SymbolNames.PartialMethod, method.Name, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SupportedDiagnostic,
                    context.Operation.Syntax.GetLocation(),
                    new[] { SymbolNames.PartialMethod }));
            }
            else if (string.Equals(SymbolNames.RenderPartialMethod, method.Name, StringComparison.Ordinal))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    SupportedDiagnostic,
                    context.Operation.Syntax.GetLocation(),
                    new[] { SymbolNames.RenderPartialMethod }));
            }
        }, OperationKind.Invocation);
    }
}
