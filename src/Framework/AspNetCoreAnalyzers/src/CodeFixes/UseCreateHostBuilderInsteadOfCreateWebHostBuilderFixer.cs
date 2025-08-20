// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.WebApplicationBuilder.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseCreateHostBuilderInsteadOfCreateWebHostBuilderFixer)), Shared]
public sealed class UseCreateHostBuilderInsteadOfCreateWebHostBuilderFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.UseCreateHostBuilderInsteadOfCreateWebHostBuilder.Id
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
            var node = root.FindNode(diagnostic.Location.SourceSpan);

            // Handle method return type case (IWebHostBuilder return type)
            if (node is TypeSyntax returnType && 
                returnType.Parent is MethodDeclarationSyntax methodDeclaration)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: "Convert to IHostBuilder and use Host.CreateDefaultBuilder",
                        createChangedDocument: c => ConvertWebHostBuilderMethod(context.Document, methodDeclaration, c),
                        equivalenceKey: "ConvertWebHostBuilderMethod"),
                    diagnostic);
            }
            
            // Handle invocation case (WebHost.CreateDefaultBuilder)
            else if (node.FirstAncestorOrSelf<InvocationExpressionSyntax>() is InvocationExpressionSyntax invocation)
            {
                if (IsWebHostCreateDefaultBuilderInvocation(invocation))
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Replace with Host.CreateDefaultBuilder",
                            createChangedDocument: c => ConvertWebHostCreateDefaultBuilder(context.Document, invocation, c),
                            equivalenceKey: "ConvertWebHostCreateDefaultBuilder"),
                        diagnostic);
                }
            }
        }
    }

    private static bool IsWebHostCreateDefaultBuilderInvocation(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            return memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: "WebHost" } &&
                   memberAccess.Name.Identifier.ValueText == "CreateDefaultBuilder";
        }
        return false;
    }

    private static async Task<Document> ConvertWebHostBuilderMethod(Document document, MethodDeclarationSyntax methodDeclaration, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) 
        {
            return document;
        }

        var newMethodDeclaration = methodDeclaration;

        // Change return type from IWebHostBuilder to IHostBuilder
        if (IsWebHostBuilderType(methodDeclaration.ReturnType))
        {
            var newReturnType = SyntaxFactory.IdentifierName("IHostBuilder");
            newMethodDeclaration = newMethodDeclaration.WithReturnType(newReturnType);
        }

        // Transform the method body to use Host.CreateDefaultBuilder and ConfigureWebHostDefaults
        if (methodDeclaration.Body != null)
        {
            var transformedBody = TransformMethodBody(methodDeclaration.Body);
            newMethodDeclaration = newMethodDeclaration.WithBody(transformedBody);
        }
        else if (methodDeclaration.ExpressionBody != null)
        {
            var transformedExpressionBody = TransformExpressionBody(methodDeclaration.ExpressionBody);
            newMethodDeclaration = newMethodDeclaration.WithExpressionBody(transformedExpressionBody);
        }

        var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration.WithLeadingTrivia(methodDeclaration.GetLeadingTrivia()));

        // Add the required using statement if not already present
        if (root is CompilationUnitSyntax compilationUnit)
        {
            var hasHostingUsing = compilationUnit.Usings.Any(u =>
                u.Name?.ToString() == "Microsoft.Extensions.Hosting");

            if (!hasHostingUsing)
            {
                var hostingUsing = SyntaxFactory.UsingDirective(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName("Microsoft"),
                            SyntaxFactory.IdentifierName("Extensions")),
                        SyntaxFactory.IdentifierName("Hosting")));

                newRoot = ((CompilationUnitSyntax)newRoot).AddUsings(hostingUsing);
            }
        }

        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsWebHostBuilderType(TypeSyntax typeSyntax)
    {
        return typeSyntax.ToString().Equals("IWebHostBuilder");
    }

    private static async Task<Document> ConvertWebHostCreateDefaultBuilder(Document document, InvocationExpressionSyntax invocation, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) 
        {
            return document;
        }

        // Find the full expression chain that contains this invocation
        var fullExpression = GetFullExpressionChain(invocation);

        SyntaxTriviaList leadingTrivia = fullExpression.GetLeadingTrivia();
        if (!fullExpression.HasLeadingTrivia)
        {
            // Try to find some leading trivia from a parent
            // e.g. using (var host = WebHost.CreateDefaultBuilder(args)) would not have leading trivia on the WebHost call
            var parent = fullExpression.Parent;
            while (parent != null && !parent.HasLeadingTrivia)
            {
                parent = parent.Parent;
            }
            leadingTrivia = parent?.GetLeadingTrivia() ?? SyntaxFactory.TriviaList();
        }

        // Transform the entire expression
        var transformedExpression = TransformExpression(fullExpression, leadingTrivia);

        var newRoot = root.ReplaceNode(fullExpression, transformedExpression);
        
        // Add the required using statement if not already present
        if (root is CompilationUnitSyntax compilationUnit)
        {
            var hasHostingUsing = compilationUnit.Usings.Any(u => 
                u.Name?.ToString() == "Microsoft.Extensions.Hosting");
                
            if (!hasHostingUsing)
            {
                var hostingUsing = SyntaxFactory.UsingDirective(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.QualifiedName(
                            SyntaxFactory.IdentifierName("Microsoft"),
                            SyntaxFactory.IdentifierName("Extensions")),
                        SyntaxFactory.IdentifierName("Hosting")));

                newRoot = ((CompilationUnitSyntax)newRoot).AddUsings(hostingUsing);
            }
        }
        
        return document.WithSyntaxRoot(newRoot);
    }

    private static ExpressionSyntax GetFullExpressionChain(ExpressionSyntax expression)
    {
        // Walk up the tree to find the root of the method call chain
        // The input expression is WebHost.CreateDefaultBuilder(), but we need to find the full chain
        var current = expression;
        
        // Walk up the parent hierarchy to find the outermost invocation in the chain
        while (current.Parent != null)
        {
            if (current.Parent is MemberAccessExpressionSyntax memberAccess && memberAccess.Expression == current)
            {
                // This current expression is the left side of a member access, so there's more to the chain
                if (memberAccess.Parent is InvocationExpressionSyntax parentInvocation)
                {
                    current = parentInvocation;
                }
                else
                {
                    break;
                }
            }
            else
            {
                // We've reached the end of the chain
                break;
            }
        }
        
        return current;
    }

    private static BlockSyntax TransformMethodBody(BlockSyntax body)
    {
        var transformedStatements = body.Statements.Select(TransformStatement).ToArray();
        return body.WithStatements(SyntaxFactory.List(transformedStatements));
    }

    private static ArrowExpressionClauseSyntax TransformExpressionBody(ArrowExpressionClauseSyntax expressionBody)
    {
        var transformedExpression = TransformExpression(expressionBody.Expression, expressionBody.Expression.GetLeadingTrivia());
        return expressionBody.WithExpression(transformedExpression);
    }

    private static StatementSyntax TransformStatement(StatementSyntax statement)
    {
        if (statement is ReturnStatementSyntax returnStatement && returnStatement.Expression != null)
        {
            var transformedExpression = TransformExpression(returnStatement.Expression,
                new SyntaxTriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed).AddRange(returnStatement.GetLeadingTrivia()).Add(SyntaxFactory.ElasticTab));
            return returnStatement.WithExpression(transformedExpression);
        }
        return statement;
    }

    private static ExpressionSyntax TransformExpression(ExpressionSyntax expression, SyntaxTriviaList leadingTrivia)
    {
        // Transform WebHost.CreateDefaultBuilder(args).ConfigureServices(...).UseStartup<Startup>()
        // to Host.CreateDefaultBuilder(args).ConfigureWebHostDefaults(webBuilder => webBuilder.ConfigureServices(...).UseStartup<Startup>())
        
        // Find the WebHost.CreateDefaultBuilder call and extract everything after it
        var (webHostCreateCall, chainedCalls, remainingChain) = ExtractWebHostBuilderChain(expression);
        
        if (webHostCreateCall != null && chainedCalls.Count > 0)
        {
            // Create Host.CreateDefaultBuilder
            var hostCreateCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Host"),
                    SyntaxFactory.IdentifierName("CreateDefaultBuilder")))
                .WithArgumentList(webHostCreateCall.ArgumentList);

            // Create the webBuilder expression chain
            var webBuilderChain = CreateWebBuilderChain(chainedCalls, leadingTrivia);

            // Create the ConfigureWebHostDefaults lambda with proper formatting
            var lambda = SyntaxFactory.SimpleLambdaExpression(
                SyntaxFactory.Parameter(SyntaxFactory.Identifier("webBuilder")),
                webBuilderChain);

            // Create Host.CreateDefaultBuilder().ConfigureWebHostDefaults(...)
            var configureCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    hostCreateCall.WithLeadingTrivia(leadingTrivia),
                    SyntaxFactory.IdentifierName("ConfigureWebHostDefaults"))
                    .WithOperatorToken(SyntaxFactory.Token(SyntaxKind.DotToken)
                        .WithLeadingTrivia(leadingTrivia)
                        ))
                .WithArgumentList(SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(lambda))))
                // Adds new line and indentation for remaining chain calls e.g. Build(), Run(), etc.
                .WithTrailingTrivia(new SyntaxTriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed).AddRange(leadingTrivia));

            // If there's a remaining chain (like .Build()), append it
            ExpressionSyntax result = configureCall;
            if (remainingChain != null)
            {
                var placeHolder = remainingChain.DescendantNodes().OfType<IdentifierNameSyntax>()
                        .FirstOrDefault(n => n.Identifier.ValueText == "HOST_PLACEHOLDER");
                if (placeHolder != null)
                {
                    // Replace the placeholder with the actual configure call
                    result = remainingChain.ReplaceNode(placeHolder, configureCall);
                }
            }

            return result;
        }
        
        // Handle standalone WebHost.CreateDefaultBuilder without chaining
        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
            memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: "WebHost" } &&
            memberAccess.Name.Identifier.ValueText == "CreateDefaultBuilder")
        {
            return SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("Host"),
                    SyntaxFactory.IdentifierName("CreateDefaultBuilder")))
                .WithArgumentList(invocation.ArgumentList)
                .WithTrailingTrivia(invocation.GetTrailingTrivia())
                .WithLeadingTrivia(invocation.GetLeadingTrivia());
        }

        return expression;
    }

    private static readonly HashSet<string> WebHostBuilderMethods = new HashSet<string>
    {
        "UseStartup",
        "ConfigureServices", 
        "ConfigureKestrel",
        "Configure",
        "UseUrls",
        "UseContentRoot",
        "UseEnvironment",
        "UseWebRoot",
        "ConfigureLogging",
        "ConfigureAppConfiguration",
        "UseIISIntegration",
        "UseIIS",
        "UseKestrel",
        "UseSockets",
        "UseQuic",
        "UseHttpSys",
        "UseDefaultServiceProvider"
    };

    private static (InvocationExpressionSyntax? webHostCreateCall, List<(SimpleNameSyntax methodName, ArgumentListSyntax arguments, SyntaxTriviaList leadingTrivia)> chainedCalls, ExpressionSyntax? remainingChain) ExtractWebHostBuilderChain(ExpressionSyntax expression)
    {
        var chainedCalls = new List<(SimpleNameSyntax methodName, ArgumentListSyntax arguments, SyntaxTriviaList leadingTrivia)>();
        InvocationExpressionSyntax? webHostCreateCall = null;
        ExpressionSyntax? remainingChain = null;
        
        // Walk the expression chain from the top level down to find all method calls
        var currentExpr = expression;
        var methodCalls = new Stack<(SimpleNameSyntax methodName, ArgumentListSyntax arguments, InvocationExpressionSyntax invocation)>();
        
        // Traverse the chain by following invocation expressions
        while (currentExpr is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name; // This preserves generic arguments
                methodCalls.Push((methodName, invocation.ArgumentList, invocation));
                
                // Move to the next expression in the chain
                currentExpr = memberAccess.Expression;
            }
            else
            {
                // This could be the WebHost.CreateDefaultBuilder call itself
                break;
            }
        }
        
        // Now process the stack to find WebHost.CreateDefaultBuilder and everything after it
        bool foundWebHostCall = false;
        var nonWebHostMethods = new List<(SimpleNameSyntax methodName, ArgumentListSyntax arguments, InvocationExpressionSyntax invocation)>();
        
        while (methodCalls.Count > 0)
        {
            var (methodName, arguments, invocationExpr) = methodCalls.Pop();
            
            if (!foundWebHostCall && methodName.Identifier.ValueText == "CreateDefaultBuilder")
            {
                // Check if this is WebHost.CreateDefaultBuilder
                if (invocationExpr.Expression is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression is IdentifierNameSyntax identifier &&
                    identifier.Identifier.ValueText == "WebHost")
                {
                    webHostCreateCall = invocationExpr;
                    foundWebHostCall = true;
                }
            }
            else if (foundWebHostCall)
            {
                // This is a method chained after WebHost.CreateDefaultBuilder
                // Check if it's a WebHostBuilder method that should go inside ConfigureWebHostDefaults
                if (WebHostBuilderMethods.Contains(methodName.Identifier.ValueText))
                {
                    chainedCalls.Add((methodName, arguments, invocationExpr.GetLeadingTrivia()));
                }
                else
                {
                    // This method should remain outside the lambda (like Build(), Run(), etc.)
                    nonWebHostMethods.Add((methodName, arguments, invocationExpr));
                    // Add any remaining methods to the non-WebHostBuilder list
                    nonWebHostMethods.AddRange(methodCalls);
                    methodCalls.Clear();
                    // Stop processing once we hit a non-WebHostBuilder method
                    break;
                }
            }
        }

        // Build the remaining chain from non-WebHostBuilder methods
        if (nonWebHostMethods.Count > 0)
        {
            // Create a placeholder for the Host.CreateDefaultBuilder().ConfigureWebHostDefaults(...) call
            // This will be replaced later, but we need something to chain the remaining methods to
            var placeholder = SyntaxFactory.IdentifierName("HOST_PLACEHOLDER");

            // Chain the remaining methods
            ExpressionSyntax current = placeholder;
            foreach (var (methodName, arguments, invocation) in nonWebHostMethods)
            {
                SyntaxTriviaList leadingTrivia = default;
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccessExpr)
                {
                    leadingTrivia = memberAccessExpr.Expression.GetLeadingTrivia();
                }

                var memberAccess = SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    // Since we're appending method calls,
                    // we need to add trailing trivia after each one to affect the new method calls formatting
                    current.WithTrailingTrivia(current.GetTrailingTrivia().AddRange(leadingTrivia)),
                    methodName);

                current = SyntaxFactory.InvocationExpression(memberAccess, arguments);
            }

            remainingChain = current;
        }

        return (webHostCreateCall, chainedCalls, remainingChain);
    }

    private static ExpressionSyntax CreateWebBuilderChain(List<(SimpleNameSyntax methodName, ArgumentListSyntax arguments, SyntaxTriviaList leadingTrivia)> chainedCalls, SyntaxTriviaList leadingTrivia2)
    {
        if (chainedCalls.Count == 0)
        {
            return SyntaxFactory.IdentifierName("webBuilder");
        }
        
        // Start with webBuilder
        ExpressionSyntax current = SyntaxFactory.IdentifierName("webBuilder");
        
        // Chain all the method calls with proper formatting
        for (int i = 0; i < chainedCalls.Count; i++)
        {
            var (methodName, arguments, leadingTrivia) = chainedCalls[i];

            var memberAccess = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                current,
                methodName); // Use the SimpleNameSyntax directly to preserve generics

            current = SyntaxFactory.InvocationExpression(memberAccess, arguments);
            current = current.WithTrailingTrivia();

            if (i < chainedCalls.Count - 1)
            {
                // Add a line break and indentation for all but the last method call
                var triviaList = new SyntaxTriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed).AddRange(leadingTrivia);
                triviaList = triviaList.Add(SyntaxFactory.ElasticTab);
                current = current.WithTrailingTrivia(triviaList);
            }
        }

        return current;
    }
}
