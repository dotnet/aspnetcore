// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.Shared;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Components.Razor
{
    internal class EventHandlerLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        public override int Order => 50;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            // For each event handler *usage* we need to rewrite the tag helper node to map to basic constructs.
            // Each usage will be represented by a tag helper property that is a descendant of either
            // a component or element.
            var references = documentNode.FindDescendantReferences<TagHelperPropertyIntermediateNode>();

            var parents = new HashSet<IntermediateNode>();
            for (var i = 0; i < references.Count; i++)
            {
                parents.Add(references[i].Parent);
            }

            foreach (var parent in parents)
            {
                ProcessDuplicates(parent);
            }

            for (var i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                var node = (TagHelperPropertyIntermediateNode)reference.Node;

                if (!reference.Parent.Children.Contains(node))
                {
                    // This node was removed as a duplicate, skip it.
                    continue;
                }

                if (node.TagHelper.IsEventHandlerTagHelper())
                {
                    reference.Replace(RewriteUsage(reference.Parent, node));
                }
            }
        }

        private void ProcessDuplicates(IntermediateNode parent)
        {
            // Reverse order because we will remove nodes.
            //
            // Each 'property' node could be duplicated if there are multiple tag helpers that match that
            // particular attribute. This is likely to happen when a component also defines something like
            // OnClick. We want to remove the 'onclick' and let it fall back to be handled by the component.
            for (var i = parent.Children.Count - 1; i >= 0; i--)
            {
                var eventHandler = parent.Children[i] as TagHelperPropertyIntermediateNode;
                if (eventHandler != null &&
                    eventHandler.TagHelper != null &&
                    eventHandler.TagHelper.IsEventHandlerTagHelper())
                {
                    for (var j = 0; j < parent.Children.Count; j++)
                    {
                        var componentAttribute = parent.Children[j] as ComponentAttributeExtensionNode;
                        if (componentAttribute != null &&
                            componentAttribute.TagHelper != null &&
                            componentAttribute.TagHelper.IsComponentTagHelper() &&
                            componentAttribute.AttributeName == eventHandler.AttributeName)
                        {
                            // Found a duplicate - remove the 'fallback' in favor of the component's own handling.
                            parent.Children.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            // If we still have duplicates at this point then they are genuine conflicts.
            var duplicates = parent.Children
                .OfType<TagHelperPropertyIntermediateNode>()
                .Where(p => p.TagHelper?.IsEventHandlerTagHelper() ?? false)
                .GroupBy(p => p.AttributeName)
                .Where(g => g.Count() > 1);

            foreach (var duplicate in duplicates)
            {
                parent.Diagnostics.Add(BlazorDiagnosticFactory.CreateEventHandler_Duplicates(
                    parent.Source,
                    duplicate.Key,
                    duplicate.ToArray()));
                foreach (var property in duplicate)
                {
                    parent.Children.Remove(property);
                }
            }
        }

        private IntermediateNode RewriteUsage(IntermediateNode parent, TagHelperPropertyIntermediateNode node)
        {
            var original = GetAttributeContent(node);
            if (original.Count == 0)
            {
                // This can happen in error cases, the parser will already have flagged this
                // as an error, so ignore it.
                return node;
            }

            // Now rewrite the content of the value node to look like:
            //
            // BindMethods.GetEventHandlerValue<TDelegate>(<code>)
            //
            // This method is overloaded on string and TDelegate, which means that it will put the code in the
            // correct context for intellisense when typing in the attribute.
            var eventArgsType = node.TagHelper.GetEventArgsType();
            var tokens = new List<IntermediateToken>()
            {
                new IntermediateToken()
                {
                    Content = $"{ComponentsApi.BindMethods.GetEventHandlerValue}<{eventArgsType}>(",
                    Kind = TokenKind.CSharp
                },
                new IntermediateToken()
                {
                    Content = $")",
                    Kind = TokenKind.CSharp
                }
            };

            for (var i = 0; i < original.Count; i++)
            {
                tokens.Insert(i + 1, original[i]);
            }

            if (parent is HtmlElementIntermediateNode)
            {
                var result = new HtmlAttributeIntermediateNode()
                {
                    AttributeName = node.AttributeName,
                    Source = node.Source,

                    Prefix = node.AttributeName + "=\"",
                    Suffix = "\"",
                };

                for (var i = 0; i < node.Diagnostics.Count; i++)
                {
                    result.Diagnostics.Add(node.Diagnostics[i]);
                }

                result.Children.Add(new CSharpExpressionAttributeValueIntermediateNode());
                for (var i = 0; i < tokens.Count; i++)
                {
                    result.Children[0].Children.Add(tokens[i]);
                }

                return result;
            }
            else
            {
                var result = new ComponentAttributeExtensionNode(node);

                result.Children.Clear();
                result.Children.Add(new CSharpExpressionIntermediateNode());
                for (var i = 0; i < tokens.Count; i++)
                {
                    result.Children[0].Children.Add(tokens[i]);
                }

                return result;
            }
        }

        private static IReadOnlyList<IntermediateToken> GetAttributeContent(TagHelperPropertyIntermediateNode node)
        {
            var template = node.FindDescendantNodes<TemplateIntermediateNode>().FirstOrDefault();
            if (template != null)
            {
                // See comments in TemplateDiagnosticPass
                node.Diagnostics.Add(BlazorDiagnosticFactory.Create_TemplateInvalidLocation(template.Source));
                return new[] { new IntermediateToken() { Kind = TokenKind.CSharp, Content = string.Empty, }, };
            }

            if (node.Children.Count == 1 && node.Children[0] is HtmlContentIntermediateNode htmlContentNode)
            {
                // This case can be hit for a 'string' attribute. We want to turn it into
                // an expression.
                var tokens = htmlContentNode.FindDescendantNodes<IntermediateToken>();

                var content = "\"" + string.Join(string.Empty, tokens.Select(t => t.Content.Replace("\"", "\\\""))) + "\"";
                return new[] { new IntermediateToken() { Content = content, Kind = TokenKind.CSharp, } };
            }
            else
            {
                return node.FindDescendantNodes<IntermediateToken>();
            }
        }
    }
}
