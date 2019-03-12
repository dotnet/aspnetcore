// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AvoidHtmlPartialAnalyzer : ViewFeatureAnalyzerBase
    {
        public AvoidHtmlPartialAnalyzer()
            : base(DiagnosticDescriptors.MVC1000_HtmlHelperPartialShouldBeAvoided)
        {
        }

        protected override void InitializeWorker(ViewFeaturesAnalyzerContext analyzerContext)
        {
            analyzerContext.Context.RegisterSyntaxNodeAction(context =>
            {
                var invocationExpression = (InvocationExpressionSyntax)context.Node;
                var symbol = context.SemanticModel.GetSymbolInfo(invocationExpression, context.CancellationToken).Symbol;
                if (symbol == null || symbol.Kind != SymbolKind.Method)
                {
                    return;
                }

                var method = (IMethodSymbol)symbol;
                if (!analyzerContext.IsHtmlHelperExtensionMethod(method))
                {
                    return;
                }

                if (string.Equals(SymbolNames.PartialMethod, method.Name, StringComparison.Ordinal))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        SupportedDiagnostic,
                        invocationExpression.GetLocation(),
                        new[] { SymbolNames.PartialMethod }));
                }
                else if (string.Equals(SymbolNames.RenderPartialMethod, method.Name, StringComparison.Ordinal))
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        SupportedDiagnostic,
                        invocationExpression.GetLocation(),
                        new[] { SymbolNames.RenderPartialMethod }));
                }
            }, SyntaxKind.InvocationExpression);
        }
    }
}
