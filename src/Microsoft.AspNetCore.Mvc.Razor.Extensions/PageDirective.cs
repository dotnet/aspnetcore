// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public static class PageDirective
    {
        public static readonly DirectiveDescriptor DirectiveDescriptor = DirectiveDescriptorBuilder
            .Create("page")
            .BeginOptionals()
            .AddString()
            .Build();

        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder)
        {
            builder.AddDirective(DirectiveDescriptor);
            return builder;
        }

        public static bool TryGetRouteTemplate(DocumentIRNode irDocument, out string routeTemplate)
        {
            var visitor = new Visitor();
            for (var i = 0; i < irDocument.Children.Count; i++)
            {
                visitor.Visit(irDocument.Children[i]);
            }

            routeTemplate = visitor.RouteTemplate;
            return visitor.DirectiveNode != null;
        }

        private class Visitor : RazorIRNodeWalker
        {
            public DirectiveIRNode DirectiveNode { get; private set; }

            public string RouteTemplate { get; private set; }

            public override void VisitDirective(DirectiveIRNode node)
            {
                if (node.Descriptor == DirectiveDescriptor)
                {
                    DirectiveNode = node;
                    RouteTemplate = node.Tokens.FirstOrDefault()?.Content;
                }
            }
        }
    }
}
