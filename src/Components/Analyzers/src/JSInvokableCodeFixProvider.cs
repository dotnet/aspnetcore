// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(JSInvokableCodeFixProvider)), Shared]
public class JSInvokableCodeFixProvider : CodeFixProvider
{
    private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.JSInvokableMethodShouldBePublic_FixTitle), Resources.ResourceManager, typeof(Resources));

    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(DiagnosticDescriptors.JSInvokableMethodShouldBePublic.Id);

    public sealed override FixAllProvider GetFixAllProvider()
    {
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return;
        }
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the method declaration identified by the diagnostic.
        var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (declaration == null)
        {
            return;
        }

        // Register a code action that will invoke the fix.
        var title = _title.ToString(CultureInfo.InvariantCulture);
        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => GetTransformedDocumentAsync(declaration, context.Document, c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> GetTransformedDocumentAsync(
        MethodDeclarationSyntax declarationNode,
        Document document,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        var updatedDeclarationNode = HandleMethodDeclaration(declarationNode);
        var newSyntaxRoot = root.ReplaceNode(declarationNode, updatedDeclarationNode);
        return document.WithSyntaxRoot(newSyntaxRoot);
    }

    private static SyntaxNode HandleMethodDeclaration(MethodDeclarationSyntax node)
    {
        var newModifiers = SyntaxFactory.TokenList(
            SyntaxFactory.Token(SyntaxKind.PublicKeyword)
        );

        foreach (var modifier in node.Modifiers)
        {
            if (modifier.IsKind(SyntaxKind.PrivateKeyword)
                || modifier.IsKind(SyntaxKind.ProtectedKeyword)
                || modifier.IsKind(SyntaxKind.InternalKeyword)
                || modifier.IsKind(SyntaxKind.PublicKeyword))
            {
                continue;
            }
            newModifiers = newModifiers.Add(modifier);
        }

        node = node.WithModifiers(newModifiers);
        return node;
    }
}
