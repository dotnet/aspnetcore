// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace Microsoft.AspNetCore.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class PublicPartialProgramClassFixer : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [DiagnosticDescriptors.PublicPartialProgramClassNotRequired.Id];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create("Remove unnecessary public partial class Program declaration",
                    async cancellationToken =>
                    {
                        var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                        var root = await context.Document.GetSyntaxRootAsync(cancellationToken);
                        if (root is null)
                        {
                            return context.Document;
                        }

                        var classDeclaration = root.FindNode(diagnostic.Location.SourceSpan)
                            .FirstAncestorOrSelf<ClassDeclarationSyntax>();
                        if (classDeclaration is null)
                        {
                            return context.Document;
                        }
                        editor.RemoveNode(classDeclaration, SyntaxRemoveOptions.KeepExteriorTrivia);
                        return editor.GetChangedDocument();
                    },
                    equivalenceKey: DiagnosticDescriptors.PublicPartialProgramClassNotRequired.Id),
                diagnostic);
        }

        return Task.CompletedTask;
    }
}
