// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class InstrumentationPass : RazorIRPassBase, IRazorIROptimizationPass
    {
        public override int Order => DefaultFeatureOrder;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            var walker = new Visitor();
            walker.VisitDocument(irDocument);

            for (var i = 0; i < walker.Items.Count; i++)
            {
                var node = walker.Items[i];
     
                AddInstrumentation(node);
            }
        }

        private static void AddInstrumentation(InstrumentationItem item)
        {
            var beginContextMethodName = "BeginContext"; /* ORIGINAL: BeginContextMethodName */
            var endContextMethodName = "EndContext"; /* ORIGINAL: EndContextMethodName */

            var beginNode = new CSharpStatementIRNode()
            {
                Parent = item.Node.Parent
            };
            RazorIRBuilder.Create(beginNode)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
                    Content = string.Format("{0}({1}, {2}, {3});",
                        beginContextMethodName,
                        item.Source.AbsoluteIndex.ToString(CultureInfo.InvariantCulture),
                        item.Source.Length.ToString(CultureInfo.InvariantCulture),
                        item.IsLiteral ? "true" : "false")
                });

            var endNode = new CSharpStatementIRNode()
            {
                Parent = item.Node.Parent
            };
            RazorIRBuilder.Create(endNode)
                .Add(new RazorIRToken()
                {
                    Kind = RazorIRToken.TokenKind.CSharp,
                    Content = string.Format("{0}();", endContextMethodName)
                });

            var nodeIndex = item.Node.Parent.Children.IndexOf(item.Node);
            item.Node.Parent.Children.Insert(nodeIndex, beginNode);
            item.Node.Parent.Children.Insert(nodeIndex + 2, endNode);
        }

        private struct InstrumentationItem
        {
            public InstrumentationItem(RazorIRNode node, bool isLiteral, SourceSpan source)
            {
                Node = node;
                IsLiteral = isLiteral;
                Source = source;
            }

            public RazorIRNode Node { get; }

            public bool IsLiteral { get; }

            public SourceSpan Source { get; }
        }

        private class Visitor : RazorIRNodeWalker
        {
            public List<InstrumentationItem> Items { get; } = new List<InstrumentationItem>();

            public override void VisitHtml(HtmlContentIRNode node)
            {
                if (node.Source != null)
                {
                    Items.Add(new InstrumentationItem(node, isLiteral: true, source: node.Source.Value));
                }

                VisitDefault(node);
            }

            public override void VisitCSharpExpression(CSharpExpressionIRNode node)
            {
                if (node.Source != null && !(node.Parent is CSharpAttributeValueIRNode))
                {
                    Items.Add(new InstrumentationItem(node, isLiteral: false, source: node.Source.Value));
                }

                VisitDefault(node);
            }

            public override void VisitTagHelper(TagHelperIRNode node)
            {
                if (node.Source != null)
                {
                    Items.Add(new InstrumentationItem(node, isLiteral: false, source: node.Source.Value));
                }

                VisitDefault(node);
            }

            public override void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIRNode node)
            {
                // We don't want to instrument TagHelper attributes. Do nothing.
            }

            public override void VisitSetTagHelperProperty(SetTagHelperPropertyIRNode node)
            {
                // We don't want to instrument TagHelper attributes. Do nothing.
            }
        }
    }
}
