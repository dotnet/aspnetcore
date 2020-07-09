// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X
{
    public class InstrumentationPass : IntermediateNodePassBase, IRazorOptimizationPass
    {
        public override int Order => DefaultFeatureOrder;

        protected override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIntermediateNode documentNode)
        {
            if (documentNode.Options.DesignTime)
            {
                return;
            }

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
            var beginContextMethodName = "BeginContext"; // ORIGINAL: BeginContextMethodName
            var endContextMethodName = "EndContext"; // ORIGINAL: EndContextMethodName

            var beginNode = new CSharpCodeIntermediateNode();
            beginNode.Children.Add(new IntermediateToken()
            {
                Kind = TokenKind.CSharp,
                Content = string.Format("{0}({1}, {2}, {3});",
                    beginContextMethodName,
                    item.Source.AbsoluteIndex.ToString(CultureInfo.InvariantCulture),
                    item.Source.Length.ToString(CultureInfo.InvariantCulture),
                    item.IsLiteral ? "true" : "false")
            });

            var endNode = new CSharpCodeIntermediateNode();
            endNode.Children.Add(new IntermediateToken()
            {
                Kind = TokenKind.CSharp,
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

                // Inside a tag helper we only want to visit inside of the body (skip all of the attributes and properties).
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    if (child is TagHelperBodyIntermediateNode || 
                        child is DefaultTagHelperBodyIntermediateNode)
                    {
                        VisitDefault(child);
                    }
                }
            }
        }
    }
}
