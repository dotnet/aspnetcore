// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.EmbeddedSyntax;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class RouteParameterAddParameterConstraintFixer : CodeFixProvider
{
    private static readonly TypeSyntax DefaultType = SyntaxFactory.ParseTypeName("string");

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.RoutePatternAddParameterConstraint.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Properties.TryGetValue("RouteParameterName", out var routeParameterName) &&
                diagnostic.Properties.TryGetValue("RouteParameterPolicy", out var routeParameterPolicy))
            {
                context.RegisterCodeFix(
                    CodeAction.Create($"Add route constraint '{routeParameterPolicy}' to '{routeParameterName}'",
                        cancellationToken => AddRouteParameterConstraintAsync(diagnostic, context.Document, cancellationToken),
                        equivalenceKey: $"{DiagnosticDescriptors.RoutePatternAddParameterConstraint.Id}-{routeParameterName}"),
                    diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> AddRouteParameterConstraintAsync(Diagnostic diagnostic, Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
        {
            return document;
        }

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
        {
            return document;
        }

        if (!WellKnownTypes.TryGetOrCreate(semanticModel.Compilation, out var wellKnownTypes))
        {
            return document;
        }

        var param = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan);
        var token = param.GetFirstToken();

        var usageContext = RoutePatternUsageDetector.BuildContext(token, semanticModel, wellKnownTypes, cancellationToken);

        var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
        var tree = RoutePatternParser.TryParse(virtualChars, supportTokenReplacement: usageContext.IsMvcAttribute);
        if (tree == null)
        {
            return document;
        }

        diagnostic.Properties.TryGetValue("RouteParameterName", out var routeParameterName);
        diagnostic.Properties.TryGetValue("RouteParameterPolicy", out var routeParameterPolicy);

        RoutePatternParameterPartNode? nameNode = null;
        var routeParameter = tree.RouteParameters[routeParameterName];
        foreach (var item in routeParameter.ParameterNode.ParameterParts)
        {
            if (item.Kind == RoutePatternKind.ParameterName)
            {
                nameNode = item;
            }
        }

        if (nameNode == null)
        {
            return document;
        }

        var newToken = CreateUpdatedToken(token, tree, nameNode, routeParameterPolicy);

        // Update document.
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
        var updatedSyntaxTree = syntaxTree.GetRoot(cancellationToken).ReplaceToken(token, newToken);
        return document.WithSyntaxRoot(updatedSyntaxTree);
    }

    private static void Split(StringBuilder sb1, StringBuilder sb2, ref bool hasSplit, EmbeddedSyntaxNode<RoutePatternKind, RoutePatternNode> node, RoutePatternParameterPartNode nameNode)
    {
        for (var i = 0; i < node.ChildCount; i++)
        {
            var child = node[i];

            if (child.IsNode)
            {
                Split(sb1, sb2, ref hasSplit, child.Node, nameNode);

                if (child.Node == nameNode)
                {
                    hasSplit = true;
                }
            }
            else
            {
                child.Token.WriteTo(!hasSplit ? sb1 : sb2);
            }
        }
    }

    private static SyntaxToken CreateUpdatedToken(SyntaxToken token, RoutePatternTree tree, RoutePatternParameterPartNode node, string constraint)
    {
        var sb1 = new StringBuilder();
        var sb2 = new StringBuilder();
        var split = false;
        Split(sb1, sb2, ref split, tree.Root, node);

        var insert = node.GetSpan().End;
        var start = token.Text.Substring(0, insert - token.SpanStart);
        var end = token.Text.Substring(insert - token.SpanStart);

        var updatedText = start + ":" + constraint + end;
        var updatedValueText = sb1.ToString() + ":" + constraint + sb2.ToString();
        var newToken = SyntaxFactory.Token(token.LeadingTrivia, token.Kind(), updatedText, updatedValueText, token.TrailingTrivia);

        return newToken;
    }
}
