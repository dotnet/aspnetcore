// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
{
    public static class ModelDirective
    {
        public static readonly DirectiveDescriptor Directive = DirectiveDescriptorBuilder.Create("model").AddType().Build();

        public static IRazorEngineBuilder Register(IRazorEngineBuilder builder)
        {
            builder.AddDirective(Directive);
            builder.Features.Add(new Pass());
            return builder;
        }

        public static string GetModelType(DocumentIRNode document)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var visitor = new Visitor();
            return GetModelType(document, visitor);
        }

        private static string GetModelType(DocumentIRNode document, Visitor visitor)
        {
            visitor.Visit(document);

            for (var i = visitor.ModelDirectives.Count - 1; i >= 0; i--)
            {
                var directive = visitor.ModelDirectives[i];

                var tokens = directive.Tokens.ToArray();
                if (tokens.Length >= 1)
                {
                    document.Parent = directive;
                    return tokens[0].Content;
                }
            }

            if (document.DocumentKind == RazorPageDocumentClassifier.RazorPageDocumentKind)
            {
                return visitor.Class.Name;
            }
            else
            {
                return  "dynamic";
            }
        }

        internal class Pass : IRazorIRPass
        {
            public RazorEngine Engine { get; set; }

            // Runs after the @inherits directive
            public int Order => RazorIRPass.DefaultDirectiveClassifierOrder + 5;

            public DocumentIRNode Execute(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
            {
                var visitor = new Visitor();
                var modelType = GetModelType(irDocument, visitor);

                var baseType = visitor.Class.BaseType;
                for (var i = visitor.InheritsDirectives.Count - 1; i >= 0; i--)
                {
                    var directive = visitor.InheritsDirectives[i];
                    var tokens = directive.Tokens.ToArray();
                    if (tokens.Length >= 1)
                    {
                        baseType = tokens[0].Content;
                        break;
                    }
                }

                visitor.Class.BaseType = baseType.Replace("<TModel>", "<" + modelType + ">");

                return irDocument;
            }
        }

        private class Visitor : RazorIRNodeWalker
        {
            public ClassDeclarationIRNode Class { get; private set; }

            public IList<DirectiveIRNode> InheritsDirectives { get; } = new List<DirectiveIRNode>();

            public IList<DirectiveIRNode> ModelDirectives { get; } = new List<DirectiveIRNode>();

            public override void VisitDocument(DocumentIRNode node)
            {
                if (node.Parent != null)
                {
                    ModelDirectives.Add((DirectiveIRNode)node.Parent);
                }
                base.VisitDocument(node);
            }

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
                    ModelDirectives.Add(node);
                }
                else if (node.Descriptor.Name == "inherits")
                {
                    InheritsDirectives.Add(node);
                }
            }
        }
    }
}
