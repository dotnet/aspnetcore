// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions
{
    public static class InjectDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptorBuilder.Create("inject").AddType().AddMember().Build();

        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder)
        {
            builder.AddDirective(Directive);
            builder.Features.Add(new Pass());
            return builder;
        }

        internal class Pass : RazorIRPassBase, IRazorDirectiveClassifierPass
        {
            public override void ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
            {
                var visitor = new Visitor();
                visitor.Visit(irDocument);
                var modelType = ModelDirective.GetModelType(irDocument);

                var properties = new HashSet<string>(StringComparer.Ordinal);

                for (var i = visitor.Directives.Count - 1; i >= 0; i--)
                {
                    var directive = visitor.Directives[i];
                    var tokens = directive.Tokens.ToArray();
                    if (tokens.Length < 2)
                    {
                        continue;
                    }

                    var typeName = tokens[0].Content;
                    var memberName = tokens[1].Content;

                    if (!properties.Add(memberName))
                    {
                        continue;
                    }

                    typeName = typeName.Replace("<TModel>", "<" + modelType + ">");

                    var injectNode = new InjectDirectiveIRNode()
                    {
                        TypeName = typeName,
                        MemberName = memberName,
                        Source = directive.Source,
                        Parent = visitor.Class,
                    };

                    visitor.Class.Children.Add(injectNode);
                }
            }
        }

        private class Visitor : RazorIRNodeWalker
        {
            public ClassDeclarationIRNode Class { get; private set; }

            public IList<DirectiveIRNode> Directives { get; } = new List<DirectiveIRNode>();

            public override void VisitClass(ClassDeclarationIRNode node)
            {
                if (Class == null)
                {
                    Class = node;
                }

                base.VisitClass(node);
            }

            public override void VisitDirective(DirectiveIRNode node)
            {
                if (node.Descriptor == Directive)
                {
                    Directives.Add(node);
                }
            }
        }
    }
}
