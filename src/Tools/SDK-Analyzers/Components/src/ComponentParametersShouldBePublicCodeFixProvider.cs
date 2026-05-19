// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Components.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ComponentParametersShouldBePublicCodeFixProvider)), Shared]
public class ComponentParametersShouldBePublicCodeFixProvider : CodeFixProvider
{
    private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ComponentParametersShouldBePublic_FixTitle), Resources.ResourceManager, typeof(Resources));

    public override ImmutableArray<string> FixableDiagnosticIds
        => ImmutableArray.Create(DiagnosticDescriptors.ComponentParametersShouldBePublic.Id);

    public sealed override FixAllProvider GetFixAllProvider()
    {
        // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics.First();
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the type declaration identified by the diagnostic.
        var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();

        // Register a code action that will invoke the fix.
        var title = Title.ToString(CultureInfo.InvariantCulture);
        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => GetTransformedDocumentAsync(context.Document, root, declaration),
                equivalenceKey: title),
            diagnostic);
    }

    private static Task<Document> GetTransformedDocumentAsync(
        Document document,
        SyntaxNode root,
        PropertyDeclarationSyntax declarationNode)
    {
        var updatedDeclarationNode = HandlePropertyDeclaration(declarationNode);
        var newSyntaxRoot = root.ReplaceNode(declarationNode, updatedDeclarationNode);
        return Task.FromResult(document.WithSyntaxRoot(newSyntaxRoot));
    }

    private static SyntaxNode HandlePropertyDeclaration(PropertyDeclarationSyntax node)
    {
        TypeSyntax type = node.Type;
        if (type == null || type.IsMissing)
        {
            return null;
        }

        var newModifiers = node.Modifiers;
        for (var i = 0; i < node.Modifiers.Count; i++)
        {
            var modifier = node.Modifiers[i];
            if (modifier.IsKind(SyntaxKind.PrivateKeyword) ||
                modifier.IsKind(SyntaxKind.ProtectedKeyword) ||
                modifier.IsKind(SyntaxKind.InternalKeyword) ||

                // We also remove public in case the user has written something totally backwards such as private public protected Foo
                modifier.IsKind(SyntaxKind.PublicKeyword))
            {
                newModifiers = newModifiers.Remove(modifier);
            }
        }

        var publicModifier = SyntaxFactory.Token(SyntaxKind.PublicKeyword);
        newModifiers = newModifiers.Insert(0, publicModifier);
        node = node.WithModifiers(newModifiers);
        return node;
    }
}
