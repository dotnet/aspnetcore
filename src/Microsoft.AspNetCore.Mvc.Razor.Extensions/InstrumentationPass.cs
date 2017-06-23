// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class InstrumentationPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        public override int Order => DefaultFeatureOrder;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            var walker = new Visitor();
            walker.VisitDocument(documentNode);

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

            var beginNode = new CSharpCodeIntermediateNode();
            beginNode.Children.Add(new IntermediateToken()
            {
                Kind = IntermediateToken.TokenKind.CSharp,
                Content = string.Format("{0}({1}, {2}, {3});",
                    beginContextMethodName,
                    item.Source.AbsoluteIndex.ToString(CultureInfo.InvariantCulture),
                    item.Source.Length.ToString(CultureInfo.InvariantCulture),
                    item.IsLiteral ? "true" : "false")
            });

            var endNode = new CSharpCodeIntermediateNode();
            endNode.Children.Add(new IntermediateToken()
            {
                Kind = IntermediateToken.TokenKind.CSharp,
                Content = string.Format("{0}();", endContextMethodName)
            });

            var nodeIndex = item.Parent.Children.IndexOf(item.Node);
            item.Parent.Children.Insert(nodeIndex, beginNode);
            item.Parent.Children.Insert(nodeIndex + 2, endNode);
        }

        private struct InstrumentationItem
        {
            public InstrumentationItem(IntermediateNode node, IntermediateNode parent, bool isLiteral, SourceSpan source)
            {
                Node = node;
                Parent = parent;
                IsLiteral = isLiteral;
                Source = source;
            }

            public IntermediateNode Node { get; }

            public IntermediateNode Parent { get; }

            public bool IsLiteral { get; }

            public SourceSpan Source { get; }
        }

        private class Visitor : IntermediateNodeWalker
        {
            public List<InstrumentationItem> Items { get; } = new List<InstrumentationItem>();

            public override void VisitHtml(HtmlContentIntermediateNode node)
            {
                if (node.Source != null)
                {
                    Items.Add(new InstrumentationItem(node, Parent, isLiteral: true, source: node.Source.Value));
                }

                VisitDefault(node);
            }

            public override void VisitCSharpExpression(CSharpExpressionIntermediateNode node)
            {
                if (node.Source != null)
                {
                    Items.Add(new InstrumentationItem(node, Parent, isLiteral: false, source: node.Source.Value));
                }

                VisitDefault(node);
            }

            public override void VisitTagHelper(TagHelperIntermediateNode node)
            {
                if (node.Source != null)
                {
                    Items.Add(new InstrumentationItem(node, Parent, isLiteral: false, source: node.Source.Value));
                }

                VisitDefault(node);
            }

            public override void VisitAddTagHelperHtmlAttribute(AddTagHelperHtmlAttributeIntermediateNode node)
            {
                // We don't want to instrument TagHelper attributes. Do nothing.
            }

            public override void VisitSetTagHelperProperty(SetTagHelperPropertyIntermediateNode node)
            {
                // We don't want to instrument TagHelper attributes. Do nothing.
            }
        }
    }
}
