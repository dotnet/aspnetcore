// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    // We use some tag helpers that can be applied directly to HTML elements. When
    // that happens, the default lowering pass will map the whole element as a tag helper.
    //
    // This phase exists to turn these 'orphan' tag helpers back into HTML elements so that
    // go down the proper path for rendering.
    internal class OrphanTagHelperLoweringPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        // Run after our other passes
        public override int Order => 1000;

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

            var visitor = new Visitor();
            visitor.Visit(documentNode);

            for (var i = 0; i < visitor.References.Count; i++)
            {
                var reference = visitor.References[i];
                var tagHelperNode = (TagHelperIntermediateNode)reference.Node;

                // Since this is converted from a tag helper to a regular old HTMl element, we need to 
                // flatten out the structure
                var insert = new List<IntermediateNode>();
                insert.Add(new HtmlContentIntermediateNode()
                {
                    Children =
                    {
                        new IntermediateToken()
                        {
                            Content = "<" + tagHelperNode.TagName + " ",
                            Kind = TokenKind.Html,
                        }
                    },
                });

                for (var j = 0; j < tagHelperNode.Diagnostics.Count; j++)
                {
                    insert[0].Diagnostics.Add(tagHelperNode.Diagnostics[j]);
                }

                // We expect to see a body node, followed by a series of property/attribute nodes
                // This isn't really the order we want, so skip over the body for now, and we'll do another
                // pass that merges it in.
                for (var j = 0; j < tagHelperNode.Children.Count; j++)
                {
                    if (tagHelperNode.Children[j] is TagHelperBodyIntermediateNode)
                    {
                        continue;
                    }
                    else if (tagHelperNode.Children[j] is TagHelperHtmlAttributeIntermediateNode htmlAttribute)
                    {
                        if (htmlAttribute.Children.Count == 0)
                        {
                            RewriteEmptyAttributeContent(insert, htmlAttribute);
                        }
                        else if (htmlAttribute.Children[0] is HtmlContentIntermediateNode)
                        {
                            RewriteHtmlAttributeContent(insert, htmlAttribute);
                        }
                        else if (htmlAttribute.Children[0] is CSharpExpressionAttributeValueIntermediateNode csharpContent)
                        {
                            RewriteCSharpAttributeContent(insert, htmlAttribute);
                        }
                    }
                    else if (tagHelperNode.Children[j] is ComponentAttributeExtensionNode attributeNode)
                    {
                        RewriteComponentAttributeContent(insert, attributeNode);
                    }
                    else
                    {
                        // We shouldn't see anything else here, but just in case, add the content as-is.
                        insert.Add(tagHelperNode.Children[j]);
                    }
                }

                if (tagHelperNode.TagMode == TagMode.SelfClosing)
                {
                    insert.Add(new HtmlContentIntermediateNode()
                    {
                        Children =
                        {
                            new IntermediateToken()
                            {
                                Content = "/>",
                                Kind = TokenKind.Html,
                            }
                        }
                    });
                }
                else if (tagHelperNode.TagMode == TagMode.StartTagOnly)
                {
                    insert.Add(new HtmlContentIntermediateNode()
                    {
                        Children =
                        {
                            new IntermediateToken()
                            {
                                Content = ">",
                                Kind = TokenKind.Html,
                            }
                        }
                    });
                }
                else
                {
                    insert.Add(new HtmlContentIntermediateNode()
                    {
                        Children =
                        {
                            new IntermediateToken()
                            {
                                Content = ">",
                                Kind = TokenKind.Html,
                            }
                        }
                    });

                    for (var j = 0; j < tagHelperNode.Children.Count; j++)
                    {
                        if (tagHelperNode.Children[j] is TagHelperBodyIntermediateNode bodyNode)
                        {
                            insert.AddRange(bodyNode.Children);
                        }
                    }

                    insert.Add(new HtmlContentIntermediateNode()
                    {
                        Children =
                        {
                            new IntermediateToken()
                            {
                                Content = "</" + tagHelperNode.TagName + ">",
                                Kind = TokenKind.Html,
                            }
                        }
                    });
                }

                reference.InsertAfter(insert);
                reference.Remove();
            }
        }
        private static void RewriteEmptyAttributeContent(List<IntermediateNode> nodes, TagHelperHtmlAttributeIntermediateNode node)
        {
            nodes.Add(new HtmlContentIntermediateNode()
            {
                Children =
                {
                    new IntermediateToken()
                    {
                        Content = node.AttributeName + " ",
                        Kind = TokenKind.Html,
                    }
                }
            });
        }

        private static void RewriteHtmlAttributeContent(List<IntermediateNode> nodes, TagHelperHtmlAttributeIntermediateNode node)
        {
            switch (node.AttributeStructure)
            {
                case AttributeStructure.Minimized:
                    nodes.Add(new HtmlContentIntermediateNode()
                    {
                        Children =
                        {
                            new IntermediateToken()
                            {
                                Content = node.AttributeName + " ",
                                Kind = TokenKind.Html,
                            }
                        }
                    });
                    break;

                // Blazor doesn't really care about preserving the fidelity of the attributes.
                case AttributeStructure.NoQuotes:
                case AttributeStructure.SingleQuotes:
                case AttributeStructure.DoubleQuotes:

                    var htmlNode = new HtmlContentIntermediateNode();
                    nodes.Add(htmlNode);

                    htmlNode.Children.Add(new IntermediateToken()
                    {
                        Content = node.AttributeName + "=\"",
                        Kind = TokenKind.Html,
                    });
                    
                    for (var i = 0; i < node.Children[0].Children.Count; i++)
                    {
                        htmlNode.Children.Add(node.Children[0].Children[i]);
                    }

                    htmlNode.Children.Add(new IntermediateToken()
                    {
                        Content = "\" ",
                        Kind = TokenKind.Html,
                    });

                    break;
            }
        }

        private static void RewriteCSharpAttributeContent(List<IntermediateNode> nodes, TagHelperHtmlAttributeIntermediateNode node)
        {
            var attributeNode = new HtmlAttributeIntermediateNode()
            {
                AttributeName = node.AttributeName,
                Prefix = "=\"",
                Suffix = "\"",
            };
            nodes.Add(attributeNode);

            var valueNode = new CSharpExpressionAttributeValueIntermediateNode();
            attributeNode.Children.Add(valueNode);

            for (var i = 0; i < node.Children[0].Children.Count; i++)
            {
                valueNode.Children.Add(node.Children[0].Children[i]);
            }
        }

        private void RewriteComponentAttributeContent(List<IntermediateNode> nodes, ComponentAttributeExtensionNode node)
        {
            var attributeNode = new HtmlAttributeIntermediateNode()
            {
                AttributeName = node.AttributeName,
                Prefix = "=\"",
                Suffix = "\"",
            };
            nodes.Add(attributeNode);

            var valueNode = new CSharpExpressionAttributeValueIntermediateNode();
            attributeNode.Children.Add(valueNode);

            for (var i = 0; i < node.Children[0].Children.Count; i++)
            {
                valueNode.Children.Add(node.Children[0].Children[i]);
            }
        }

        private class Visitor : IntermediateNodeWalker
        {
            public List<IntermediateNodeReference> References = new List<IntermediateNodeReference>();

            public override void VisitTagHelper(TagHelperIntermediateNode node)
            {
                base.VisitTagHelper(node);

                // Use a post-order traversal because we're going to rewrite tag helper nodes, and thus
                // change the parent nodes.
                //
                // This ensures that we operate the leaf nodes first.
                if (!node.TagHelpers.Any(t => t.IsComponentTagHelper()))
                {
                    References.Add(new IntermediateNodeReference(Parent, node));
                }
            }
        }
    }
}
