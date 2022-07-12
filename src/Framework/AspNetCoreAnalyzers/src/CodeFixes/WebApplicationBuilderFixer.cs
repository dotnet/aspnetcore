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
        DiagnosticDescriptors.DoNotUseHostConfigureServices.Id,
        DiagnosticDescriptors.DoNotUseHostConfigureLogging.Id,
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
        // hey Safia some of the below are just draft comments to help you understand the code easier.
        // We're gonna delete it later before the final commit
        
        DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root == null)
        {
            return document;
        }

        var diagnosticTarget = root.FindNode(diagnostic.Location.SourceSpan); //whole diagnosticTarget starting from "builder"
      
        if (diagnosticTarget is InvocationExpressionSyntax { } originalInvocation)
        {
            if (originalInvocation.Expression is not MemberAccessExpressionSyntax hostBasedInvocationMethodExpr
                || hostBasedInvocationMethodExpr.Expression is not MemberAccessExpressionSyntax subExpression)
            {
                return editor.OriginalDocument;
            }

            var identifierMethod = SyntaxFactory.IdentifierName(IDENTIFIER_METHOD_NAME);

            if (subExpression.Name != null) //subExpression.Name is Host/Webhost
            {
                subExpression = subExpression.WithName(identifierMethod);
                hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithExpression(subExpression); //becomes builder.Configuration.ConfigureAppConfiguration
            }

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
                        || currentStatement.Expression is not InvocationExpressionSyntax body)
                    //currentStatement is builder.AddJsonFile() or builder.AddEnvironmentVariables()
                    //body is builder.AddJsonFile() or builder.AddEnvironmentVariables()
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
                    originalInvocation = originalInvocation.Update(hostBasedInvocationMethodExpr, argument); 
                    hostBasedInvocationMethodExpr = hostBasedInvocationMethodExpr.WithExpression(originalInvocation);


                    //first iteration: adding AddJsonFile method

                    //line 104: from builder.Configuration.ConfigureAppConfiguration() to builder.Configuration.AddJsonFile
                    //line 105: from original diagnosticTarget to builder.Configuration.AddJsonFile(new arguments)
                    //line 106: from builder.Configuration.AddJsonFile to builder.Configuration.AddJsonFile(new arguments).AddJsonFile

                    //second iteration: adding AddEnvironmentVariables method after AddJsonFile
                    //line 104: from builder.Configuration.AddJsonFile(new arguments).AddJsonFile to builder.Configuration.AddJsonFile(new arguments).AddEnvironmentVariables
                    //line 105: from builder.Configuration.AddJsonFile(new arguments) to builder.Configuration.AddJsonFile(new arguments).AddenvironmentVariables()
                    //line 106: from builder.Configuration.AddJsonFile(new arguments).AddEnvironmentVariables to builder.Configuration.AddJsonFile(new arguments).AddEnvironmentVariables().AddEnvironmentVariables

                    //so basically line 106 is just setting up for the next iteration, where the duplicated method is gonna be replaced with a new method
                    //so the second AddJsonFile will be replaced with AddEnvironmentVariables
                    //after the last iteration hostBasedInvocationMethodExpr will become an diagnosticTarget with duplicated methods, which doesn't make any sense
                    //but since the originalInvocation is already updated with the correct value, we're not gonna need hostBasedInvocationMethodExpr anymore
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
