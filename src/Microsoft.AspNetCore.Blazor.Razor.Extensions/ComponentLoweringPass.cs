// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    internal class ComponentLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after our *special* tag helpers get lowered.
        public override int Order => 1000;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var @namespace = documentNode.FindPrimaryNamespace();
            var @class = documentNode.FindPrimaryClass();
            if (@namespace == null || @class == null)
            {
                // Nothing to do, bail. We can't function without the standard structure.
                return;
            }

            // For each component *usage* we need to rewrite the tag helper node to map to the relevant component
            // APIs.
            var nodes = documentNode.FindDescendantNodes<TagHelperIntermediateNode>();
            for (var i = 0; i < nodes.Count; i++)
            {
                var count = 0;
                var node = nodes[i];
                for (var j = 0; j < node.TagHelpers.Count; j++)
                {
                    if (node.TagHelpers[j].IsComponentTagHelper())
                    {
                        // Only allow a single component tag helper per element. We also have some *special* tag helpers
                        // and they should have already been processed by now.
                        if (count++ > 1)
                        {
                            node.Diagnostics.Add(BlazorDiagnosticFactory.Create_MultipleComponents(node.Source, node.TagName, node.TagHelpers));
                            break;
                        }

                        RewriteUsage(node, node.TagHelpers[j]);
                    }
                }
            }
        }

        private void RewriteUsage(TagHelperIntermediateNode node, TagHelperDescriptor tagHelper)
        {
            // We need to surround the contents of the node with open and close nodes to ensure the component
            // is scoped correctly.
            node.Children.Insert(0, new ComponentOpenExtensionNode()
            {
                TypeName = tagHelper.GetTypeName(),
            });

            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                if (node.Children[i] is TagHelperBodyIntermediateNode bodyNode)
                {
                    // Replace with a node that we recognize so that it we can do proper scope tracking.
                    //
                    // Note that we force the body node to be last, this is done to push it after the
                    // attribute nodes. This gives us the ordering we want for the render tree.
                    node.Children.RemoveAt(i);
                    node.Children.Add(new ComponentBodyExtensionNode(bodyNode)
                    {
                        TagMode = node.TagMode,
                        TagName = node.TagName,
                    });
                }
            }

            node.Children.Add(new ComponentCloseExtensionNode());

            // Now we need to rewrite any set property or HTML nodes to call the appropriate AddAttribute api.
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                if (node.Children[i] is TagHelperPropertyIntermediateNode propertyNode &&
                    propertyNode.TagHelper == tagHelper)
                {
                    // We don't support 'complex' content for components (mixed C# and markup) right now.
                    // It's not clear yet if Blazor will have a good scenario to use these constructs.
                    //
                    // This is where a lot of the complexity in the Razor/TagHelpers model creeps in and we
                    // might be able to avoid it if these features aren't needed.
                    if (HasComplexChildContent(propertyNode))
                    {
                        node.Diagnostics.Add(BlazorDiagnosticFactory.Create_UnsupportedComplexContent(
                            propertyNode,
                            propertyNode.AttributeName));
                        node.Children.RemoveAt(i);
                        continue;
                    }

                    node.Children[i] = new ComponentAttributeExtensionNode(propertyNode);
                }
                else if (node.Children[i] is TagHelperHtmlAttributeIntermediateNode htmlNode)
                {
                    if (HasComplexChildContent(htmlNode))
                    {
                        node.Diagnostics.Add(BlazorDiagnosticFactory.Create_UnsupportedComplexContent(
                            htmlNode,
                            htmlNode.AttributeName));
                        node.Children.RemoveAt(i);
                        continue;
                    }

                    // For any nodes that don't map to a component property we won't have type information
                    // but these should follow the same path through the runtime.
                    var attributeNode = new ComponentAttributeExtensionNode(htmlNode);
                    node.Children[i] = attributeNode;

                    // Since we don't support complex content, we can rewrite the inside of this
                    // node to the rather simpler form that property nodes usually have.
                    for (var j = 0; j < attributeNode.Children.Count; j++)
                    {
                        if (attributeNode.Children[j] is HtmlAttributeValueIntermediateNode htmlValue)
                        {
                            attributeNode.Children[j] = new HtmlContentIntermediateNode()
                            {
                                Children =
                                {
                                    htmlValue.Children.Single(),
                                },
                                Source = htmlValue.Source,
                            };
                        }
                        else if (attributeNode.Children[j] is CSharpExpressionAttributeValueIntermediateNode expressionValue)
                        {
                            attributeNode.Children[j] = new CSharpExpressionIntermediateNode()
                            {
                                Children =
                                {
                                    expressionValue.Children.Single(),
                                },
                                Source = expressionValue.Source,
                            };
                        }
                        else if (attributeNode.Children[j] is CSharpCodeAttributeValueIntermediateNode codeValue)
                        {
                            attributeNode.Children[j] = new CSharpExpressionIntermediateNode()
                            {
                                Children =
                                {
                                    codeValue.Children.Single(),
                                },
                                Source = codeValue.Source,
                            };
                        }
                    }
                }
            }
        }

        private static bool HasComplexChildContent(IntermediateNode node)
        {
            if (node.Children.Count == 1 &&
                node.Children[0] is HtmlAttributeIntermediateNode htmlNode &&
                htmlNode.Children.Count > 1)
            {
                // This case can be hit for a 'string' attribute
                return true;
            }
            else if (node.Children.Count == 1 &&
                node.Children[0] is CSharpExpressionIntermediateNode cSharpNode &&
                cSharpNode.Children.Count > 1)
            {
                // This case can be hit when the attribute has an explicit @ inside, which
                // 'escapes' any special sugar we provide for codegen.
                return true;
            }
            else if (node.Children.Count > 1)
            {
                // This is the common case for 'mixed' content
                return true;
            }

            return false;
        }
    }
}
