// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Components.Analyzers
{
    /// <summary>
    /// This API supports infrastructure and is not intended to be used
    /// directly from your code. This API may change or be removed in future releases.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ComponentClosureOverLoopVariablesDiagnosticAnalzyer : DiagnosticAnalyzer
    {

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticDescriptors.ClosureOverLoopVariables);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
            context.RegisterCompilationStartAction(context =>
            {
                context.RegisterSyntaxNodeAction(AnalyzeForLoop, SyntaxKind.ForStatement);
            });
        }

        private void AnalyzeForLoop(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode syntaxNode = context.Node;

            if (syntaxNode is ForStatementSyntax forSyntax)
            { 
                var variableIdentifier = forSyntax
                    .DescendantTokens()
                    .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
                    .FirstOrDefault()
                    .Text;
                
                if (String.IsNullOrWhiteSpace(variableIdentifier))
                {
                    return;
                }

                var lambdaExpressions = forSyntax
                    .DescendantNodes()
                    .OfType<LambdaExpressionSyntax>();

                foreach (var expression in lambdaExpressions)
                {
                    var indentifiers = expression
                        .DescendantNodes()
                        .OfType<IdentifierNameSyntax>();

                    foreach (var identifierName in indentifiers)
                    {
                        if (identifierName.Identifier.Text == variableIdentifier)
                        {
                            context.ReportDiagnostic(Diagnostic.Create(
                                        DiagnosticDescriptors.ClosureOverLoopVariables,
                                        identifierName.GetLocation(),
                                        expression.GetText().ToString()));
                        }
                    }
                }
            }
        }
    }
}
