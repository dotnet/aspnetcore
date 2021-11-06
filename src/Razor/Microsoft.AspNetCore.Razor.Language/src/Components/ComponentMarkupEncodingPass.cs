// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.AspNetCore.Razor.Language.Legacy;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal class ComponentMarkupEncodingPass : ComponentIntermediateNodePassBase, IRazorOptimizationPass
{
    // Runs after ComponentMarkupBlockPass
    public override int Order => ComponentMarkupDiagnosticPass.DefaultOrder + 20;

    protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
    {
        if (!IsComponentDocument(documentNode))
        {
            return;
        }

        if (documentNode.Options.DesignTime)
        {
            // Nothing to do during design time.
            return;
        }

        var rewriter = new Rewriter();
        rewriter.Visit(documentNode);
    }

    private class Rewriter : IntermediateNodeWalker
    {
        // Markup content in components are rendered in one of the following two ways,
        // AddContent - we encode it when used with prerendering and inserted into the DOM in a safe way (low perf impact)
        // AddMarkupContent - renders the content directly as markup (high perf impact)
        // Because of this, we want to use AddContent as much as possible.
        //
        // We want to use AddMarkupContent to avoid aggresive encoding during prerendering.
        // Specifically, when one of the following characters are in the content,
        // 1. New lines (\r, \n), tabs(\t) - so they get rendered as actual new lines, tabs instead of &#xA;
        // 2. Any character outside the ASCII range

        private static readonly char[] EncodedCharacters = new[] { '\r', '\n', '\t' };

        private readonly Dictionary<string, string> _seenEntities = new Dictionary<string, string>(StringComparer.Ordinal);

        public override void VisitHtml(HtmlContentIntermediateNode node)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                if (!(child is IntermediateToken token) || !token.IsHtml || string.IsNullOrEmpty(token.Content))
                {
                    // We only care about Html tokens.
                    continue;
                }

                for (var j = 0; j < token.Content.Length; j++)
                {
                    var ch = token.Content[j];
                    // ASCII range is 0 - 127
                    if (ch > 127 || EncodedCharacters.Contains(ch))
                    {
                        node.SetEncoded();
                        return;
                    }
                }
            }

            // If we reach here, we don't have newlines, tabs or non-ascii characters in this node.
            // If we can successfully decode all HTML entities(if any) in this node, we can safely let it call AddContent.
            var decodedContent = new string[node.Children.Count];
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                if (!(child is IntermediateToken token) || !token.IsHtml || string.IsNullOrEmpty(token.Content))
                {
                    // We only care about Html tokens.
                    continue;
                }

                if (TryDecodeHtmlEntities(token.Content, out var decoded))
                {
                    decodedContent[i] = decoded;
                }
                else
                {
                    node.SetEncoded();
                    return;
                }
            }

            // If we reach here, it means we have successfully decoded all content.
            // Replace all token content with the decoded value.
            for (var i = 0; i < node.Children.Count; i++)
            {
                var child = node.Children[i];
                if (!(child is IntermediateToken token) || !token.IsHtml || string.IsNullOrEmpty(token.Content))
                {
                    // We only care about Html tokens.
                    continue;
                }

                token.Content = decodedContent[i];
            }
        }

        private bool TryDecodeHtmlEntities(string content, out string decoded)
        {
            _seenEntities.Clear();
            decoded = content;
            var i = 0;
            while (i < content.Length)
            {
                var ch = content[i];
                if (ch == '&')
                {
                    if (TryGetHtmlEntity(content, i, out var entity, out var replacement))
                    {
                        if (!_seenEntities.ContainsKey(entity))
                        {
                            _seenEntities.Add(entity, replacement);
                        }

                        i += entity.Length;
                    }
                    else
                    {
                        // We found a '&' that we don't know what to do with. Don't try to decode further.
                        return false;
                    }
                }
                else
                {
                    i++;
                }
            }

            foreach (var entity in _seenEntities)
            {
                decoded = decoded.Replace(entity.Key, entity.Value);
            }

            return true;
        }

        private bool TryGetHtmlEntity(string content, int position, out string entity, out string replacement)
        {
            // We're at '&'. Check if it is the start of an HTML entity.
            entity = null;
            replacement = null;
            var endPosition = -1;
            for (var i = position + 1; i < content.Length; i++)
            {
                var ch = content[i];
                if (char.IsLetterOrDigit(ch) || ch == '#')
                {
                    continue;
                }
                else if (ch == ';')
                {
                    endPosition = i;
                }

                break;
            }

            if (endPosition != -1)
            {
                entity = content.Substring(position, endPosition - position + 1);
                if (entity.StartsWith("&#", StringComparison.Ordinal))
                {
                    // Extract the codepoint and map it to an entity.

                    // `entity` is guaranteed to be of the format &#****;
                    var entityValue = entity.Substring(2, entity.Length - 3);
                    int codePoint;
                    if (!int.TryParse(entityValue, out codePoint))
                    {
                        // If it is not an integer, check if it is hexadecimal like 0x00CD
                        try
                        {
                            codePoint = Convert.ToInt32(entityValue, 16);
                        }
                        catch (FormatException)
                        {
                            // Do nothing.
                        }
                    }

                    if (ParserHelpers.HtmlEntityCodePoints.TryGetValue(codePoint, out replacement))
                    {
                        // This is a known html entity unicode codepoint.
                        return true;
                    }

                    // Unknown entity.
                    return false;
                }
                else if (ParserHelpers.NamedHtmlEntities.TryGetValue(entity, out replacement))
                {
                    return true;
                }
            }

            // The '&' is not part of an HTML entity.
            return false;
        }
    }
}
