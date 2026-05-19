// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(WebApplicationBuilderFixer)), Shared]
public sealed class WebApplicationBuilderFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        // Add other diagnostic descriptor id's
        DiagnosticDescriptors.DoNotUseHostConfigureLogging.Id,
        DiagnosticDescriptors.DoNotUseHostConfigureServices.Id,
        DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id
     );

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }

        foreach (var diagnostic in context.Diagnostics)
        {
            var id = diagnostic.Id;

            var message = string.Empty;
            var identifierMethod = string.Empty;

            switch (id)
            {
                case string when id == DiagnosticDescriptors.DoNotUseHostConfigureLogging.Id:
                    message = "Fix references to Logging properties on WebApplicationBuilder";
                    identifierMethod = "Logging";
                    break;

                case string when id == DiagnosticDescriptors.DoNotUseHostConfigureServices.Id:
                    message = "Fix references to Services properties on WebApplicationBuilder";
                    identifierMethod = "Services";
                    break;

                case string when id == DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id:
                    message = "Fix references to Configuration properties on WebApplicationBuilder";
                    identifierMethod = "Configuration";
                    break;
            }

            if (!CanFixWebApplicationBuilder(diagnostic, SyntaxFactory.IdentifierName(identifierMethod), root, out var invocation))
            {
                continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    message,
                    cancellationToken => FixWebApplicationBuilderAsync(diagnostic, root, context.Document, invocation),
                    equivalenceKey:
                    id),
                    diagnostic);
        }
    }

    private static Task<Document> FixWebApplicationBuilderAsync(Diagnostic diagnostic, SyntaxNode root, Document document, InvocationExpressionSyntax invocation)
    {
        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        return Task.FromResult(document.WithSyntaxRoot(root.ReplaceNode(diagnosticTarget, invocation)));
    }

    private static bool CanFixWebApplicationBuilder(Diagnostic diagnostic, IdentifierNameSyntax identifierMethod, SyntaxNode root, [NotNullWhen(true)] out InvocationExpressionSyntax? invocationName)
    {
        invocationName = null;

        if (root == null)
        {
            return false;
        }

        // builder.Host.ConfigureLogging(builder => builder.AddJsonConsole());
        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

        if (diagnosticTarget is InvocationExpressionSyntax invocation)
        {
            // No modification are made if the invocation isn't accessing a method on `builder.Host` or `builder.WebHost`.
            if (invocation.Expression is not MemberAccessExpressionSyntax hostBasedInvocationMethodExpr
                || hostBasedInvocationMethodExpr.Expression is not MemberAccessExpressionSyntax configureMethodOnHostAccessExpr)
            {
                return false;
            }

            configureMethodOnHostAccessExpr = configureMethodOnHostAccessExpr.WithName(identifierMethod);
            var indentation = hostBasedInvocationMethodExpr.GetLeadingTrivia();

            // builder.Host.ConfigureLogging => builder.Logging
            // builder.WebHost.ConfigureServices => builder.Services
            hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithExpression(configureMethodOnHostAccessExpr)
                .NormalizeWhitespace().WithLeadingTrivia(indentation);

            if (invocation.ArgumentList.Arguments.SingleOrDefault() is not { } initArgument
                || initArgument.Expression is not LambdaExpressionSyntax lambdaExpr)
            {
                return false;
            }

            if (lambdaExpr.Block != null)
            {
                var lambdaStatements = lambdaExpr.Block.Statements;
                foreach (var statement in lambdaStatements)
                {
                    if (statement is not ExpressionStatementSyntax currentStatement
                        || currentStatement.Expression is not InvocationExpressionSyntax expr)
                    {
                        return false;
                    }

                    // arguments of builder.{method_name}({arguments})
                    var argument = expr.ArgumentList;

                    if (expr.Expression is not MemberAccessExpressionSyntax bodyExpression) //builder.{method_name}
                    {
                        return false;
                    }

                    var method = bodyExpression.Name; // method_name

                    hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithName(method);
                    invocation = invocation.Update(hostBasedInvocationMethodExpr, argument);
                    hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithExpression(invocation);
                }
            }
            else
            {
                if (lambdaExpr.ExpressionBody is not InvocationExpressionSyntax body)
                {
                    return false;
                }

                var arguments = body.ArgumentList;

                if (body.Expression is not MemberAccessExpressionSyntax bodyExpression)
                {
                    return false;
                }

                var method = bodyExpression.Name;

                hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithName(method);
                invocation = invocation.WithExpression(hostBasedInvocationMethodExpr).WithArgumentList(arguments);
            }
            invocationName = invocation;
            return true;
        }
        return false;
    }
}
