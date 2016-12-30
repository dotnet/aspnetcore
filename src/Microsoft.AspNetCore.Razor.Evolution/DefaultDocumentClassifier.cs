// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultDocumentClassifier : RazorIRPassBase
    {
        public override int Order => RazorIRPass.DefaultDocumentClassifierOrder;

        public static string DocumentKind = "default";

        public override DocumentIRNode ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            if (irDocument.DocumentKind != null)
            {
                return irDocument;
            }

            irDocument.DocumentKind = DocumentKind;

            // Rewrite a use default namespace and class declaration.
            var children = new List<RazorIRNode>(irDocument.Children);
            irDocument.Children.Clear();

            var @namespace = new NamespaceDeclarationIRNode()
            {
                //Content = "GeneratedNamespace",
            };

            var @class = new ClassDeclarationIRNode()
            {
                //AccessModifier = "public",
                //Name = "GeneratedClass",
            };

            var method = new RazorMethodDeclarationIRNode()
            {
                //AccessModifier = "public",
               // Modifiers = new List<string>() { "async" },
                //Name = "Execute",
                //ReturnType = "Task",
            };

            var documentBuilder = RazorIRBuilder.Create(irDocument);

            var namespaceBuilder = RazorIRBuilder.Create(documentBuilder.Current);
            namespaceBuilder.Push(@namespace);

            var classBuilder = RazorIRBuilder.Create(namespaceBuilder.Current);
            classBuilder.Push(@class);

            var methodBuilder = RazorIRBuilder.Create(classBuilder.Current);
            methodBuilder.Push(method);

            var visitor = new Visitor(documentBuilder, namespaceBuilder, classBuilder, methodBuilder);

            for (var i = 0; i < children.Count; i++)
            {
                visitor.Visit(children[i]);
            }

            return irDocument;
        }

        private class Visitor : RazorIRNodeVisitor
        {
            private readonly RazorIRBuilder _document;
            private readonly RazorIRBuilder _namespace;
            private readonly RazorIRBuilder _class;
            private readonly RazorIRBuilder _method;

            public Visitor(RazorIRBuilder document, RazorIRBuilder @namespace, RazorIRBuilder @class, RazorIRBuilder method)
            {
                _document = document;
                _namespace = @namespace;
                _class = @class;
                _method = method;
            }

            public override void VisitChecksum(ChecksumIRNode node)
            {
                _document.Insert(0, node);
            }
            
            public override void VisitUsingStatement(UsingStatementIRNode node)
            {
                _namespace.AddAfter<UsingStatementIRNode>(node);
            }

            internal override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIRNode node)
            {
                _class.Insert(0, node);
            }

            public override void VisitDefault(RazorIRNode node)
            {
                _method.Add(node);
            }
        }
       
        public void Foo()
        {
            //// For prettiness, let's insert the usings before the class declaration.
            //var i = 0;
            //for (; i < Namespace.Children.Count; i++)
            //{
            //    if (Namespace.Children[i] is ClassDeclarationIRNode)
            //    {
            //        break;
            //    }
            //}

            //var @using = new UsingStatementIRNode()
            //{
            //    Content = namespaceImport,
            //    SourceRange = BuildSourceRangeFromNode(span),
            //};
        }
    }
}
