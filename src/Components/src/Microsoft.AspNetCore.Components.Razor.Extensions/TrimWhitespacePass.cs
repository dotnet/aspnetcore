// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using System;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class TrimWhitespacePass : IntermediateNodePassBase, IRazorDirectiveClassifierPass
    {
        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (codeDocument == null)
            {
                throw new ArgumentNullException(nameof(codeDocument));
            }

            if (documentNode == null)
            {
                throw new ArgumentNullException(nameof(documentNode));
            }

            // There's no benefit running the whitespace trimmer during design-time builds
            if (!documentNode.Options.DesignTime)
            {
                var method = documentNode.FindPrimaryMethod();
                if (method != null)
                {
                    RemoveContiguousWhitespace(method.Children, TraversalDirection.Forwards);
                    RemoveContiguousWhitespace(method.Children, TraversalDirection.Backwards);
                }
            }
        }

        private static void RemoveContiguousWhitespace(IntermediateNodeCollection nodes, TraversalDirection direction)
        {
            var position = direction == TraversalDirection.Forwards ? 0 : nodes.Count - 1;
            while (position >= 0 && position < nodes.Count)
            {
                var node = nodes[position];
                bool shouldRemoveNode;
                bool shouldContinueIteration;

                switch (node)
                {
                    case IntermediateToken intermediateToken:
                        shouldRemoveNode = string.IsNullOrWhiteSpace(intermediateToken.Content);
                        shouldContinueIteration = shouldRemoveNode;
                        break;

                    case HtmlContentIntermediateNode htmlContentIntermediateNode:
                        RemoveContiguousWhitespace(htmlContentIntermediateNode.Children, direction);
                        shouldRemoveNode = htmlContentIntermediateNode.Children.Count == 0;
                        shouldContinueIteration = shouldRemoveNode;
                        break;

                    case HtmlElementIntermediateNode _:
                    case CSharpExpressionIntermediateNode _:
                    case TagHelperIntermediateNode _:
                        // These node types may produce non-whitespace output at runtime
                        shouldRemoveNode = false;
                        shouldContinueIteration = false;
                        break;

                    case CSharpCodeIntermediateNode codeIntermediateNode:
                        shouldRemoveNode = false;
                        shouldContinueIteration = ComponentDocumentClassifierPass.IsBuildRenderTreeBaseCall(codeIntermediateNode);
                        break;

                    default:
                        shouldRemoveNode = false;
                        shouldContinueIteration = true; // Because other types of nodes don't produce output
                        break;
                }

                if (shouldRemoveNode)
                {
                    nodes.RemoveAt(position);
                    if (direction == TraversalDirection.Forwards)
                    {
                        position--;
                    }
                }

                position += direction == TraversalDirection.Forwards ? 1 : -1;

                if (!shouldContinueIteration)
                {
                    break;
                }
            }
        }

        enum TraversalDirection
        {
            Forwards,
            Backwards
        }
    }
}
