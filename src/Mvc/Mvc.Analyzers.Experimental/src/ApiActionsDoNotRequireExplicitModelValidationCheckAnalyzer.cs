// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.AspNetCore.Mvc.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzer : ApiControllerAnalyzerBase
    {
        public ApiActionsDoNotRequireExplicitModelValidationCheckAnalyzer()
            : base(DiagnosticDescriptors.MVC7001_ApiActionsHaveBadModelStateFilter)
        {
        }

        protected override void InitializeWorker(ApiControllerAnalyzerContext analyzerContext)
        {
            analyzerContext.Context.RegisterSyntaxNodeAction(context =>
            {
                var methodSyntax = (MethodDeclarationSyntax)context.Node;
                if (methodSyntax.Body == null)
                {
                    // Ignore expression bodied methods.
                    return;
                }

                var method = context.SemanticModel.GetDeclaredSymbol(methodSyntax, context.CancellationToken);
                if (!analyzerContext.IsApiAction(method))
                {
                    return;
                }

                if (method.ReturnsVoid || method.ReturnType == analyzerContext.SystemThreadingTaskOfT)
                {
                    // Void or Task returning methods. We don't have to check anything here since we're specifically
                    // looking for return BadRequest(..);
                    return;
                }

                // Only look for top level statements that look like "if (!ModelState.IsValid)"
                foreach (var memberAccessSyntax in methodSyntax.Body.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
                {
                    var ancestorIfStatement = memberAccessSyntax.FirstAncestorOrSelf<IfStatementSyntax>();
                    if (ancestorIfStatement == null)
                    {
                        // Node's not in an if statement.
                        continue;
                    }

                    var symbolInfo = context.SemanticModel.GetSymbolInfo(memberAccessSyntax, context.CancellationToken);

                    if (!(symbolInfo.Symbol is IPropertySymbol property) ||
                        (property.ContainingType != analyzerContext.ModelStateDictionary) ||
                        !string.Equals(property.Name, "IsValid", StringComparison.Ordinal) ||
                        !IsFalseExpression(memberAccessSyntax))
                    {
                        continue;
                    }

                    var containingBlock = (SyntaxNode)ancestorIfStatement;
                    if (containingBlock.Parent.Kind() == SyntaxKind.ElseClause)
                    {
                        containingBlock = containingBlock.Parent;
                    }
                    context.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostic, containingBlock.GetLocation()));
                    return;
                }
            }, SyntaxKind.MethodDeclaration);
        }

        private static bool IsFalseExpression(MemberAccessExpressionSyntax memberAccessSyntax)
        {
            switch (memberAccessSyntax.Parent.Kind())
            {
                case SyntaxKind.LogicalNotExpression:
                    // !ModelState.IsValid
                    return true;
                case SyntaxKind.EqualsExpression:
                    var binaryExpression = (BinaryExpressionSyntax)memberAccessSyntax.Parent;
                    // ModelState.IsValid == false
                    // false == ModelState.IsValid
                    return binaryExpression.Left.Kind() == SyntaxKind.FalseLiteralExpression ||
                        binaryExpression.Right.Kind() == SyntaxKind.FalseLiteralExpression;
            }

            return false;
        }
    }
}
