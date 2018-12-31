// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Components
{
    internal class ComponentWhitespacePass : ComponentIntermediateNodePassBase, IRazorDirectiveClassifierPass
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

            if (!IsComponentDocument(documentNode))
            {
                return;
            }

            if (documentNode.Options.SuppressPrimaryMethodBody)
            {
                // There's no benefit running the whitespace trimmer if we're not emitting
                // the method bodies.
                return;
            }

            // There's no benefit running the whitespace trimmer during design-time builds
            if (documentNode.Options.DesignTime)
            {
                return;
            }

            var method = documentNode.FindPrimaryMethod();
            if (method != null)
            {
                RemoveContiguousWhitespace(method.Children, TraversalDirection.Forwards);
                RemoveContiguousWhitespace(method.Children, TraversalDirection.Backwards);
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

                    case MarkupElementIntermediateNode _:
                    case CSharpExpressionIntermediateNode _:
                    case TagHelperIntermediateNode _:
                        // These node types may produce non-whitespace output at runtime
                        shouldRemoveNode = false;
                        shouldContinueIteration = false;
                        break;

                    case CSharpCodeIntermediateNode codeIntermediateNode:
                        // A C# code node could be empty. We can't remove them, but we can skip them. 
                        shouldRemoveNode = false;
                        shouldContinueIteration = 
                            IsEmpty(codeIntermediateNode) || 
                            ComponentDocumentClassifierPass.IsBuildRenderTreeBaseCall(codeIntermediateNode);
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

        private static bool IsEmpty(CSharpCodeIntermediateNode node)
        {
            for (var i = 0; i < node.Children.Count; i++)
            {
                if (!(node.Children[i] is IntermediateToken token && string.IsNullOrWhiteSpace(token.Content)))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
