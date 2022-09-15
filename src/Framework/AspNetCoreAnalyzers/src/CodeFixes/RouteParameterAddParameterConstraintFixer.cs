// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Composition;
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

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Fixers;

[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public class RouteParameterAddParameterConstraintFixer : CodeFixProvider
{
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
                        cancellationToken => AddRouteParameterConstraintAsync(diagnostic, context.Document, routeParameterName, routeParameterPolicy, cancellationToken),
                        equivalenceKey: $"{DiagnosticDescriptors.RoutePatternAddParameterConstraint.Id}-{routeParameterName}"),
                    diagnostic);
            }
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> AddRouteParameterConstraintAsync(
        Diagnostic diagnostic, Document document, string routeParameterName, string routeParameterPolicy, CancellationToken cancellationToken)
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

        var newToken = CreateUpdatedToken(token, tree, routeParameterName, routeParameterPolicy);
        if (newToken == null)
        {
            return document;
        }

        // Update document.
        var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
        var updatedSyntaxTree = syntaxTree.GetRoot(cancellationToken).ReplaceToken(token, newToken.Value);
        return document.WithSyntaxRoot(updatedSyntaxTree);
    }

    private static SyntaxToken? CreateUpdatedToken(SyntaxToken token, RoutePatternTree tree, string routeParameterName, string constraint)
    {
        // The constraint is added after the parameter name. To update the route string token:
        // 1. Find parameter name node.
        // 2. Iterate through route parameter nodes, find the parameter name, and get before and after strings.
        // 3. Combine before string, new constraint, and after string.
        var nameNode = FindParameterNameNode(tree, routeParameterName);

        var beforeValueText = new StringBuilder();
        var afterValueText = new StringBuilder();
        var split = false;
        var splitPosition = 0;
        Split(beforeValueText, afterValueText, ref split, ref splitPosition, tree.Root, nameNode);

        if (!split)
        {
            return null;
        }

        var beforeText = token.Text.Substring(0, splitPosition - token.SpanStart);
        var afterText = token.Text.Substring(splitPosition - token.SpanStart);

        var updatedText = beforeText + ":" + constraint + afterText;
        var updatedValueText = beforeValueText.ToString() + ":" + constraint + afterValueText.ToString();
        var newToken = SyntaxFactory.Token(token.LeadingTrivia, token.Kind(), updatedText, updatedValueText, token.TrailingTrivia);

        return newToken;

        static void Split(StringBuilder before, StringBuilder after, ref bool hasSplit, ref int splitPosition, EmbeddedSyntaxNode<RoutePatternKind, RoutePatternNode> node, RoutePatternParameterPartNode splitNode)
        {
            for (var i = 0; i < node.ChildCount; i++)
            {
                var child = node[i];

                if (child.IsNode)
                {
                    Split(before, after, ref hasSplit, ref splitPosition, child.Node, splitNode);

                    if (child.Node == splitNode)
                    {
                        hasSplit = true;
                        splitPosition = child.Node.GetSpan().End;
                    }
                }
                else
                {
                    child.Token.WriteTo(!hasSplit ? before : after);
                }
            }
        }

        static RoutePatternParameterPartNode? FindParameterNameNode(RoutePatternTree tree, string routeParameterName)
        {
            var routeParameter = tree.GetRouteParameter(routeParameterName);
            foreach (var item in routeParameter.ParameterNode.ParameterParts)
            {
                if (item.Kind == RoutePatternKind.ParameterName)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
