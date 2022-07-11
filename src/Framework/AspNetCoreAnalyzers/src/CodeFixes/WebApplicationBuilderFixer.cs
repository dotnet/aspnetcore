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
        DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id
     });

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create("Fix Configure methods",
                    cancellationToken => FixConfigureMethods(diagnostic, context.Document, cancellationToken),
                    equivalenceKey: DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixConfigureMethods(Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
    {
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return document;
        }

        var invocation = root.FindNode(diagnostic.Location.SourceSpan);
      
        if (invocation is InvocationExpressionSyntax { } newInvocationExpr)
        {
            if (newInvocationExpr.Expression is MemberAccessExpressionSyntax { } invokedMethodExpr)
            {
                var configuration = SyntaxFactory.IdentifierName("Configuration");
                if (invokedMethodExpr.Expression is MemberAccessExpressionSyntax { } subExpression)
                {
                    if (subExpression.Name != null)
                    {
                        subExpression = subExpression.WithName(configuration);
                        invokedMethodExpr = invokedMethodExpr.WithExpression(subExpression);
                    }

                    if (newInvocationExpr.ArgumentList.Arguments.Count == 0)
                    {
                        return editor.GetChangedDocument();
                    }

                    static BlockSyntax ExtractArgumentBody(InvocationExpressionSyntax invocationExpression)
                    {
                        var initArgument = invocationExpression.ArgumentList.Arguments[0];
                        if (initArgument.Expression is LambdaExpressionSyntax { } lambdaExpression)
                        {
                            var body = lambdaExpression.Block;
                            var attribute = lambdaExpression.Block.AttributeLists;
                            var bla = lambdaExpression.Block.Statements;
                            return body;
                        }
                        return null;
                    }
                    var exprBodies = ExtractArgumentBody(newInvocationExpr);
                    //if (ExtractArgumentBody(newInvocationExpr) is InvocationExpressionSyntax { } body)
                    //{
                    //    var argument = body.ArgumentList;
                    //    if (body.Expression is MemberAccessExpressionSyntax { } bodyExpression)
                    //    {
                    //        var method = bodyExpression.Name;

                    //        invokedMethodExpr = invokedMethodExpr.WithName(method);
                    //        newInvocationExpr = newInvocationExpr.WithExpression(invokedMethodExpr).WithArgumentList(argument);
                    //    }
                    //}

                    editor.ReplaceNode(invocation, newInvocationExpr);
                }
            }
        }

        return editor.GetChangedDocument();
    }
}
