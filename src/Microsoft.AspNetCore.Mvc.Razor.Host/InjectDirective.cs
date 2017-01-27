// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
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

        private class Pass : IRazorIRPass
        {
            public RazorEngine Engine { get; set; }

            public int Order => RazorIRPass.DirectiveClassifierOrder;

            public DocumentIRNode Execute(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
            {
                var visitor = new Visitor();
                visitor.Visit(irDocument);

                for (var i = 0; i < visitor.Directives.Count; i++)
                {
                    var directive = visitor.Directives[i];
                    var typeName = directive.Tokens.ElementAt(0).Content;;
                    var memberName = directive.Tokens.ElementAt(1).Content;

                    var modelType = "dynamic";
                    if (visitor.ModelType.Count > 0)
                    {
                        modelType = visitor.ModelType.Last().Tokens.First().Content;
                    }

                    typeName = typeName.Replace("<TModel>", "<" + modelType + ">");

                    var member = new CSharpStatementIRNode()
                    {
                        Source = directive.Source,
                        Content = $"[Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]{Environment.NewLine}public {typeName} {memberName} {{ get; private set; }}",
                        Parent = visitor.Class,
                    };

                    visitor.Class.Children.Add(member);
                }

                return irDocument;
            }
        }

        private class Visitor : RazorIRNodeWalker
        {
            public ClassDeclarationIRNode Class { get; private set; }

            public IList<DirectiveIRNode> Directives { get; } = new List<DirectiveIRNode>();

            public IList<DirectiveIRNode> ModelType { get; } = new List<DirectiveIRNode>();

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
                else if (node.Descriptor == ModelDirective.Directive)
                {
                    ModelType.Add(node);
                }
            }
        }
    }
}
