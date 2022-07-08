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
      
        if (invocation is InvocationExpressionSyntax { } invocationExpression)
        {
            var expression = invocationExpression.Expression as MemberAccessExpressionSyntax;
            var configuration = SyntaxFactory.IdentifierName("Configuration");
            var builder = expression.Expression as MemberAccessExpressionSyntax;

            builder = builder.WithName(configuration);
            expression = expression.WithExpression(builder);

            var initArgument = invocationExpression.ArgumentList.Arguments[0];
            var lambdaExpression = initArgument.Expression as SimpleLambdaExpressionSyntax;
            var body = lambdaExpression.ExpressionBody as InvocationExpressionSyntax;
            var argument = body.ArgumentList;
            var bodyExpression = body.Expression as MemberAccessExpressionSyntax;
            var method = bodyExpression.Name;

            expression = expression.WithName(method);
            invocationExpression = invocationExpression.WithExpression(expression);
            invocationExpression = invocationExpression.WithArgumentList(argument);

            editor.ReplaceNode(invocation, invocationExpression);
        }

        return editor.GetChangedDocument();
    }
}
