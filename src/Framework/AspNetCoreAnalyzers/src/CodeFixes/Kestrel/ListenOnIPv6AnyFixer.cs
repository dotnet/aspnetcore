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
                        throw new System.NotImplementedException();

                        //var editor = await DocumentEditor.CreateAsync(context.Document, cancellationToken).ConfigureAwait(false);
                        //var root = await context.Document.GetSyntaxRootAsync(cancellationToken);
                        //if (root is null)
                        //{
                        //    return context.Document;
                        //}

                        //var classDeclaration = root.FindNode(diagnostic.Location.SourceSpan)
                        //    .FirstAncestorOrSelf<ClassDeclarationSyntax>();
                        //if (classDeclaration is null)
                        //{
                        //    return context.Document;
                        //}
                        //editor.RemoveNode(classDeclaration, SyntaxRemoveOptions.KeepExteriorTrivia);
                        //return editor.GetChangedDocument();

                    },
                    equivalenceKey: DiagnosticDescriptors.KestrelShouldListenOnIPv6AnyInsteadOfIpAny.Id),
                diagnostic);
        }

        return Task.CompletedTask;
    }
}
