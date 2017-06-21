// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public class PageDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptor.CreateDirective(
            "page",
            DirectiveKind.SingleLine,
            builder =>
            {
                builder.AddOptionalStringToken();
                builder.Usage = DirectiveUsage.FileScopedSinglyOccurring;
            });

        private PageDirective(string routeTemplate)
        {
            RouteTemplate = routeTemplate;
        }

        public string RouteTemplate { get; }

        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder)
        {
            builder.AddDirective(Directive);
            return builder;
        }

        public static bool TryGetPageDirective(DocumentIntermediateNode documentNode, out PageDirective pageDirective)
        {
            var visitor = new Visitor();
            for (var i = 0; i < documentNode.Children.Count; i++)
            {
                visitor.Visit(documentNode.Children[i]);
            }

            if (visitor.DirectiveNode == null)
            {
                pageDirective = null;
                return false;
            }

            var tokens = visitor.DirectiveNode.Tokens.ToList();
            string routeTemplate = null;
            if (tokens.Count > 0)
            {
                routeTemplate = TrimQuotes(tokens[0].Content);
            }

            pageDirective = new PageDirective(routeTemplate);
            return true;
        }

        private static string TrimQuotes(string content)
        {
            Debug.Assert(content.Length >= 2);
            Debug.Assert(content.StartsWith("\"", StringComparison.Ordinal));
            Debug.Assert(content.EndsWith("\"", StringComparison.Ordinal));

            return content.Substring(1, content.Length - 2);
        }

        private class Visitor : IntermediateNodeWalker
        {
            public DirectiveIntermediateNode DirectiveNode { get; private set; }

            public override void VisitDirective(DirectiveIntermediateNode node)
            {
                if (node.Descriptor == Directive)
                {
                    DirectiveNode = node;
                }
            }
        }
    }
}
