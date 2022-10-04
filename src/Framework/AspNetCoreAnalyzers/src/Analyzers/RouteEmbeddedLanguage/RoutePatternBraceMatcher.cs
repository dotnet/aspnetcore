// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Threading;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.VirtualChars;
using Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.AspNetCore.EmbeddedLanguages;
using RoutePatternToken = Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.Infrastructure.EmbeddedSyntax.EmbeddedSyntaxToken<Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage.RoutePattern.RoutePatternKind>;

namespace Microsoft.AspNetCore.Analyzers.RouteEmbeddedLanguage;

[ExportAspNetCoreEmbeddedLanguageBraceMatcher(name: "Route", language: LanguageNames.CSharp)]
internal class RoutePatternBraceMatcher : IAspNetCoreEmbeddedLanguageBraceMatcher
{
    public AspNetCoreBraceMatchingResult? FindBraces(SemanticModel semanticModel, SyntaxToken token, int position, CancellationToken cancellationToken)
    {
        if (!WellKnownTypes.TryGetOrCreate(semanticModel.Compilation, out var wellKnownTypes))
        {
            return null;
        }

        var usageContext = RoutePatternUsageDetector.BuildContext(token, semanticModel, wellKnownTypes, cancellationToken);

        var virtualChars = CSharpVirtualCharService.Instance.TryConvertToVirtualChars(token);
        var tree = RoutePatternParser.TryParse(virtualChars, supportTokenReplacement: usageContext.IsMvcAttribute);
        if (tree == null)
        {
            return null;
        }

        return GetMatchingBraces(tree, position);
    }

    private static AspNetCoreBraceMatchingResult? GetMatchingBraces(RoutePatternTree tree, int position)
    {
        var virtualChar = tree.Text.Find(position);
        if (virtualChar == null)
        {
            return null;
        }

        var ch = virtualChar.Value;
        return ch.Value switch
        {
            '{' or '}' => FindParameterBraces(tree, ch),
            '(' or ')' => FindPolicyParens(tree, ch),
            '[' or ']' => FindReplacementTokenBrackets(tree, ch),
            _ => null,
        };
    }

    private static AspNetCoreBraceMatchingResult? FindParameterBraces(RoutePatternTree tree, VirtualChar ch)
    {
        var node = FindParameterNode(tree.Root, ch);
        return node == null ? null : CreateResult(node.OpenBraceToken, node.CloseBraceToken);
    }

    private static AspNetCoreBraceMatchingResult? FindPolicyParens(RoutePatternTree tree, VirtualChar ch)
    {
        var node = FindPolicyFragmentEscapedNode(tree.Root, ch);
        return node == null ? null : CreateResult(node.OpenParenToken, node.CloseParenToken);
    }

    private static AspNetCoreBraceMatchingResult? FindReplacementTokenBrackets(RoutePatternTree tree, VirtualChar ch)
    {
        var node = FindReplacementNode(tree.Root, ch);
        return node == null ? null : CreateResult(node.OpenBracketToken, node.CloseBracketToken);
    }

    private static RoutePatternParameterNode? FindParameterNode(RoutePatternNode node, VirtualChar ch)
        => FindNode<RoutePatternParameterNode>(node, ch, (parameter, c) =>
                parameter.OpenBraceToken.VirtualChars.Contains(c) || parameter.CloseBraceToken.VirtualChars.Contains(c));

    private static RoutePatternPolicyFragmentEscapedNode? FindPolicyFragmentEscapedNode(RoutePatternNode node, VirtualChar ch)
        => FindNode<RoutePatternPolicyFragmentEscapedNode>(node, ch, (fragment, c) =>
                fragment.OpenParenToken.VirtualChars.Contains(c) || fragment.CloseParenToken.VirtualChars.Contains(c));

    private static RoutePatternReplacementNode? FindReplacementNode(RoutePatternNode node, VirtualChar ch)
        => FindNode<RoutePatternReplacementNode>(node, ch, (fragment, c) =>
                fragment.OpenBracketToken.VirtualChars.Contains(c) || fragment.CloseBracketToken.VirtualChars.Contains(c));

    private static TNode? FindNode<TNode>(RoutePatternNode node, VirtualChar ch, Func<TNode, VirtualChar, bool> predicate)
        where TNode : RoutePatternNode
    {
        if (node is TNode nodeMatch && predicate(nodeMatch, ch))
        {
            return nodeMatch;
        }

        foreach (var child in node)
        {
            if (child.IsNode)
            {
                var result = FindNode(child.Node, ch, predicate);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

    private static AspNetCoreBraceMatchingResult? CreateResult(RoutePatternToken open, RoutePatternToken close)
        => open.IsMissing || close.IsMissing
            ? null
            : new AspNetCoreBraceMatchingResult(open.VirtualChars[0].Span, close.VirtualChars[0].Span);

    // IAspNetCoreEmbeddedLanguageBraceMatcher is internal and tests don't have access to it. Provide a way to get its assembly.
    // Just for unit tests. Don't use in production code.
    internal static class TestAccessor
    {
        public static Assembly ExternalAccessAssembly => typeof(IAspNetCoreEmbeddedLanguageBraceMatcher).Assembly;
    }
}
