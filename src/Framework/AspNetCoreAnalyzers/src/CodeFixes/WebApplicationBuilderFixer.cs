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
using System.Linq;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers;

public class WebApplicationBuilderFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        // Add other diagnostic descriptor id's
        DiagnosticDescriptors.DoNotUseHostConfigureLogging.Id,
        DiagnosticDescriptors.DoNotUseHostConfigureServices.Id,
        DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id
     );

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
                        CodeAction.Create(
                            "Fix references to Logging properties on WebApplicationBuilder",
                            cancellationToken => FixWebApplicationBuilder(diagnostic, context.Document, cancellationToken, SyntaxFactory.IdentifierName("Logging")),
                            equivalenceKey:
                            DiagnosticDescriptors.DoNotUseHostConfigureLogging.Id),
                            diagnostic);
                    break;

                case string when id == DiagnosticDescriptors.DoNotUseHostConfigureServices.Id:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Fix references to Services properties on WebApplicationBuilder",
                            cancellationToken => FixWebApplicationBuilder(diagnostic, context.Document, cancellationToken, SyntaxFactory.IdentifierName("Services")),
                            equivalenceKey:
                            DiagnosticDescriptors.DoNotUseHostConfigureServices.Id),
                            diagnostic);
                    break;

                case string when id == DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            "Fix references to Configuration properties on WebApplicationBuilder",
                            cancellationToken => FixWebApplicationBuilder(diagnostic, context.Document, cancellationToken, SyntaxFactory.IdentifierName("Configuration")),
                            equivalenceKey:
                            DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id),
                            diagnostic);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixWebApplicationBuilder(Diagnostic diagnostic, Document document, CancellationToken cancellationToken, IdentifierNameSyntax identifierMethod)
    {
        //var editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return document;
        }

        //whole invocation starting from "builder"
        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan); 

        if (diagnosticTarget is InvocationExpressionSyntax originalInvocation)
        {
            // if builder.{Host or WebHost}.{method_name} or builder.{Host or WebHost}
            // is not of type MemberAccessExpressionSyntax, return original code
            if (originalInvocation.Expression is not MemberAccessExpressionSyntax hostBasedInvocationMethodExpr
                || hostBasedInvocationMethodExpr.Expression is not MemberAccessExpressionSyntax subExpression)
            {
                return document;
            }

            subExpression = subExpression.WithName(identifierMethod);
            var indentation = hostBasedInvocationMethodExpr.GetLeadingTrivia();
            // replace Host/WebHost with identifierMethod
            hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithExpression(subExpression)
                .NormalizeWhitespace().WithLeadingTrivia(indentation); 

            if (originalInvocation.ArgumentList.Arguments.Count == 0)
            {
                return document;
            }

            var initArgumentExpr = originalInvocation.ArgumentList.Arguments.SingleOrDefault().Expression; // builder => { }

            if (initArgumentExpr is not LambdaExpressionSyntax lambdaExpr)
            {
                return document;
            }

            if (lambdaExpr.Block != null)
            {
                var lambdaStatements = lambdaExpr.Block.Statements;
                foreach (var statement in lambdaStatements)
                {
                    if (statement is not ExpressionStatementSyntax currentStatement
                        || currentStatement.Expression is not InvocationExpressionSyntax expr)
                    {
                        return document;
                    }

                    // arguments of builder.{method_name}({arguments})
                    var argument = expr.ArgumentList; 

                    if (expr.Expression is not MemberAccessExpressionSyntax bodyExpression) //builder.{method_name} 
                    {
                        return document;
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
                    return document;
                }

                var arguments = body.ArgumentList;

                if (body.Expression is not MemberAccessExpressionSyntax bodyExpression)
                {
                    return document;
                }

                var method = bodyExpression.Name;

                hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithName(method);
                originalInvocation = originalInvocation.WithExpression(hostBasedInvocationMethodExpr).WithArgumentList(arguments);
            }
            else
            {
                return document;
            }

            return document.WithSyntaxRoot(root.ReplaceNode(diagnosticTarget, originalInvocation));
        }
        return document;
    }
}
