// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Composition;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzers;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Microsoft.AspNetCore.Fixers.Kestrel;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class ListenOnIPv6AnyFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [ DiagnosticDescriptors.KestrelShouldListenOnIPv6AnyInsteadOfIpAny.Id ];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    "Consider using IPAddress.IPv6Any instead of IPAddress.Any",
                    async cancellationToken =>
                    {
                        var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                        var root = await context.Document.GetSyntaxRootAsync(cancellationToken);
                        if (root is null)
                        {
                            return context.Document;
                        }

                        var argumentSyntax = root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<ArgumentSyntax>();
                        if (argumentSyntax is null)
                        {
                            return context.Document;
                        }

                        // get to the `Listen(IPAddress.Any, ...)` invocation
                        if (argumentSyntax.Parent?.Parent is not InvocationExpressionSyntax { ArgumentList.Arguments.Count: > 1 } invocationExpressionSyntax)
                        {
                            return context.Document;
                        }
                        if (invocationExpressionSyntax.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
                        {
                            return context.Document;
                        }

                        var instanceVariableInvoked = memberAccessExpressionSyntax.Expression;
                        var adjustedArgumentList = invocationExpressionSyntax.ArgumentList.RemoveNode(invocationExpressionSyntax.ArgumentList.Arguments.First(), SyntaxRemoveOptions.KeepLeadingTrivia);
                        if (adjustedArgumentList is null || adjustedArgumentList.Arguments.Count == 0)
                        {
                            return context.Document;
                        }

                        // changing invocation from `<variable>.Listen(IPAddress.Any, ...)` to `<variable>.ListenAnyIP(...)`
                        editor.ReplaceNode(
                            invocationExpressionSyntax,
                            invocationExpressionSyntax
                                .WithExpression(SyntaxFactory.ParseExpression($"{instanceVariableInvoked.ToString()}.ListenAnyIP"))
                                .WithArgumentList(adjustedArgumentList!)
                                .WithLeadingTrivia(invocationExpressionSyntax.GetLeadingTrivia())
                        );
                        return editor.GetChangedDocument();
                    },
                    equivalenceKey: DiagnosticDescriptors.KestrelShouldListenOnIPv6AnyInsteadOfIpAny.Id),
                diagnostic);
        }

        return Task.CompletedTask;
    }
}
