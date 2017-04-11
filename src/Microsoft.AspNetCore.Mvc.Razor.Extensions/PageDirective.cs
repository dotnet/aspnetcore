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
        public static readonly DirectiveDescriptor DirectiveDescriptor = DirectiveDescriptorBuilder
            .Create("page")
            .BeginOptionals()
            .AddString() // Route template
            .AddString() // Page Name
            .Build();

        private PageDirective(string routeTemplate, string pageName)
        {
            RouteTemplate = routeTemplate;
            PageName = pageName;
        }

        public string RouteTemplate { get; }

        public string PageName { get; }

        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder)
        {
            builder.AddDirective(DirectiveDescriptor);
            return builder;
        }

        public static bool TryGetPageDirective(DocumentIRNode irDocument, out PageDirective pageDirective)
        {
            var visitor = new Visitor();
            for (var i = 0; i < irDocument.Children.Count; i++)
            {
                visitor.Visit(irDocument.Children[i]);
            }

            if (visitor.DirectiveNode == null)
            {
                pageDirective = null;
                return false;
            }

            var tokens = visitor.DirectiveNode.Tokens.ToList();
            string routeTemplate = null;
            string pageName = null;
            if (tokens.Count > 0)
            {
                routeTemplate = TrimQuotes(tokens[0].Content);
            }

            if (tokens.Count > 1)
            {
                pageName = TrimQuotes(tokens[1].Content);
            }

            pageDirective = new PageDirective(routeTemplate, pageName);
            return true;
        }

        private static string TrimQuotes(string content)
        {
            Debug.Assert(content.StartsWith("\"", StringComparison.Ordinal));
            Debug.Assert(content.EndsWith("\"", StringComparison.Ordinal));

            return content.Substring(1, content.Length - 2);
        }

        private class Visitor : RazorIRNodeWalker
        {
            public DirectiveIRNode DirectiveNode { get; private set; }

            public override void VisitDirective(DirectiveIRNode node)
            {
                if (node.Descriptor == DirectiveDescriptor)
                {
                    DirectiveNode = node;
                }
            }
        }
    }
}
