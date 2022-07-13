// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers;
public class WebApplicationBuilderFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(new[]
    {
        // Add other diagnostic descriptor id's
        DiagnosticDescriptors.DoNotUseHostConfigureLogging.Id,
        DiagnosticDescriptors.DoNotUseHostConfigureServices.Id,
        DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id
     });

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {

        foreach (var diagnostic in context.Diagnostics)
        {
            string id = diagnostic.Id;
            switch (id)
            {
                case string when id == DiagnosticDescriptors.DoNotUseHostConfigureLogging.Id:
                    context.RegisterCodeFix(
                    CodeAction.Create("Fix references to Logging properties on WebApplicationBuilder",
                    cancellationToken => FixWebApplicationBuilder(diagnostic, context.Document, cancellationToken, "Logging"),
                    equivalenceKey: DiagnosticDescriptors.DoNotUseHostConfigureLogging.Id),
                    diagnostic);
                    break;

                case string when id == DiagnosticDescriptors.DoNotUseHostConfigureServices.Id:
                    context.RegisterCodeFix(
                    CodeAction.Create("Fix references to Services properties on WebApplicationBuilder",
                    cancellationToken => FixWebApplicationBuilder(diagnostic, context.Document, cancellationToken, "Services"),
                    equivalenceKey: DiagnosticDescriptors.DoNotUseHostConfigureServices.Id),
                    diagnostic);
                    break;

                case string when id == DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id:
                    context.RegisterCodeFix(
                    CodeAction.Create("Fix references to Services properties on WebApplicationBuilder",
                    cancellationToken => FixWebApplicationBuilder(diagnostic, context.Document, cancellationToken, "Configuration"),
                    equivalenceKey: DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id),
                    diagnostic);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixWebApplicationBuilder(Diagnostic diagnostic, Document document, CancellationToken cancellationToken, string IDENTIFIER_METHOD_NAME)
    {

        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return document;
        }

        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan); //whole invocation starting from "builder"

        if (diagnosticTarget is InvocationExpressionSyntax { } originalInvocation)
        {
            if (originalInvocation.Expression is not MemberAccessExpressionSyntax hostBasedInvocationMethodExpr
                || hostBasedInvocationMethodExpr.Expression is not MemberAccessExpressionSyntax subExpression)
            {
                return editor.OriginalDocument;
            }

            var identifierMethod = SyntaxFactory.IdentifierName(IDENTIFIER_METHOD_NAME);

            subExpression = subExpression.WithName(identifierMethod);
            var indentation = hostBasedInvocationMethodExpr.GetLeadingTrivia();
            hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithExpression(subExpression)
                .NormalizeWhitespace().WithLeadingTrivia(indentation); // replace Host/WebHost with identifierMethod

            if (originalInvocation.ArgumentList.Arguments.Count == 0)
            {
                return editor.OriginalDocument;
            }

            var initArgumentExpr = originalInvocation.ArgumentList.Arguments[0].Expression; // builder => { }

            if (initArgumentExpr is not LambdaExpressionSyntax lambdaExpr)
            {
                return editor.OriginalDocument;
            }

            if (lambdaExpr.Block != null)
            {
                var lambdaStatements = lambdaExpr.Block.Statements;
                foreach (var statement in lambdaStatements)
                {
                    if (statement is not ExpressionStatementSyntax currentStatement
                        || currentStatement.Expression is not InvocationExpressionSyntax expr)
                    {
                        return editor.OriginalDocument;
                    }

                    var argument = expr.ArgumentList; // arguments of builder.{method_name}({arguments})

                    if (expr.Expression is not MemberAccessExpressionSyntax bodyExpression) //builder.{method_name} 
                    {
                        return editor.OriginalDocument;
                    }

                    var method = bodyExpression.Name; // method_name

                    hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithName(method);
                    originalInvocation = originalInvocation.Update(hostBasedInvocationMethodExpr, argument);
                    hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithExpression(originalInvocation);

                }
            }

            else if (lambdaExpr.ExpressionBody != null)
            {
                if (lambdaExpr.ExpressionBody is not InvocationExpressionSyntax body)
                {
                    return editor.OriginalDocument;
                }

                var argument = body.ArgumentList;

                if (body.Expression is not MemberAccessExpressionSyntax bodyExpression)
                {
                    return editor.OriginalDocument;
                }

                var method = bodyExpression.Name;

                hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithName(method);
                originalInvocation = originalInvocation.WithExpression(hostBasedInvocationMethodExpr).WithArgumentList(argument);
            }

            else
            {
                return editor.OriginalDocument;
            }

            editor.ReplaceNode(diagnosticTarget, originalInvocation);
        }

        return editor.GetChangedDocument();
    }
}
