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
                    cancellationToken => FixConfigureMethods(diagnostic, context.Document, cancellationToken, "Configuration"),
                    equivalenceKey: DiagnosticDescriptors.DisallowConfigureAppConfigureHostBuilder.Id),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> FixConfigureMethods(Diagnostic diagnostic, Document document, CancellationToken cancellationToken, string IDENTIFIER_METHOD_NAME)
    {
        // hey Safia some of the below are just draft comments to help you understand the code easier.
        // We're gonna delete it later before the final commit
        
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return document;
        }

        var invocation = root.FindNode(diagnostic.Location.SourceSpan); //whole invocation starting from "builder"
      
        if (invocation is InvocationExpressionSyntax { } newInvocationExpr)
        {
            if (newInvocationExpr.Expression is not MemberAccessExpressionSyntax hostBasedInvocationMethodExpr)
            {
                return editor.OriginalDocument;
            }

            var identifierMethod = SyntaxFactory.IdentifierName(IDENTIFIER_METHOD_NAME);

            if (hostBasedInvocationMethodExpr.Expression is not MemberAccessExpressionSyntax subExpression)
            {
                return editor.OriginalDocument;
            }
            if (subExpression.Name != null) //subExpression.Name is Host/Webhost
            {
                subExpression = subExpression.WithName(identifierMethod);
                hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithExpression(subExpression); //becomes builder.Configuration.ConfigureAppConfiguration
            }

            if (newInvocationExpr.ArgumentList.Arguments.Count == 0)
            {
                return editor.OriginalDocument;
            }

            var initArgumentExpr = newInvocationExpr.ArgumentList.Arguments[0].Expression; // builder => { }

            if (initArgumentExpr is ParenthesizedLambdaExpressionSyntax { } parenLambdaExpr)
            {
                var bodyEnum = ExtractParenLambdaBody(parenLambdaExpr).GetEnumerator();
                while (bodyEnum.MoveNext())
                {
                    if (bodyEnum.Current is not ExpressionStatementSyntax currentStatement) //builder.AddJsonFile() or builder.AddEnvironmentVariables()
                    {
                        return editor.OriginalDocument;
                    }

                    if (currentStatement.Expression is not InvocationExpressionSyntax body) //builder.AddJsonFile() or builder.AddEnvironmentVariables()
                    {
                        return editor.OriginalDocument;
                    }

                    var argument = body.ArgumentList; //arguments of builder.AddJsonFile() or builder.AddEnvironmentVariables()

                    if (body.Expression is not MemberAccessExpressionSyntax bodyExpression) //builder.AddJsonFile or builder.AddEnvironmentVariables
                    {
                        return editor.OriginalDocument;
                    }

                    var method = bodyExpression.Name; //AddJsonFile or AddEnvironmentVariables

                    hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithName(method); 
                    newInvocationExpr = newInvocationExpr.Update(hostBasedInvocationMethodExpr, argument); 
                    hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithExpression(newInvocationExpr);


                    //first iteration: adding AddJsonFile method

                    //line 104: from builder.Configuration.ConfigureAppConfiguration() to builder.Configuration.AddJsonFile
                    //line 105: from original invocation to builder.Configuration.AddJsonFile(new arguments)
                    //line 106: from builder.Configuration.AddJsonFile to builder.Configuration.AddJsonFile(new arguments).AddJsonFile

                    //second iteration: adding AddEnvironmentVariables method after AddJsonFile
                    //line 104: from builder.Configuration.AddJsonFile(new arguments).AddJsonFile to builder.Configuration.AddJsonFile(new arguments).AddEnvironmentVariables
                    //line 105: from builder.Configuration.AddJsonFile(new arguments) to builder.Configuration.AddJsonFile(new arguments).AddenvironmentVariables()
                    //line 106: from builder.Configuration.AddJsonFile(new arguments).AddEnvironmentVariables to builder.Configuration.AddJsonFile(new arguments).AddEnvironmentVariables().AddEnvironmentVariables

                    //so basically line 106 is just setting up for the next iteration, where the duplicated method is gonna be replaced with a new method
                    //so the second AddJsonFile will be replaced with AddEnvironmentVariables
                    //after the last iteration hostBasedInvocationMethodExpr will become an invocation with duplicated methods, which doesn't make any sense
                    //but since the newInvocationExpr is already updated with the correct value, we're not gonna need hostBasedInvocationMethodExpr anymore
                }
            }

            else if (initArgumentExpr is SimpleLambdaExpressionSyntax { } simpleLambdaExpr)
            {
                if (ExtractSimpleLambdaBody(simpleLambdaExpr) is not InvocationExpressionSyntax body)
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
                newInvocationExpr = newInvocationExpr.WithExpression(hostBasedInvocationMethodExpr).WithArgumentList(argument);
            }

            editor.ReplaceNode(invocation, newInvocationExpr);
        }

        return editor.GetChangedDocument();
    }
    private static SyntaxList<StatementSyntax> ExtractParenLambdaBody(ParenthesizedLambdaExpressionSyntax initArgumentExpr)
    {
        var body = initArgumentExpr.Block.Statements;

        return body;
    }

    private static ExpressionSyntax ExtractSimpleLambdaBody(SimpleLambdaExpressionSyntax initArgumentExpr)
    {
        var body = initArgumentExpr.ExpressionBody;

        return body;
    }
}
