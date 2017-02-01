// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
{
    public abstract class BaseDocumentClassifierPass : IRazorIRPass
    {
        public RazorEngine Engine { get; set; }

        // We want to run before the default, but after others since this is the MVC default.
        public virtual int Order => RazorIRPass.DefaultDocumentClassifierOrder - 1;

        protected abstract string BaseType { get; }

        public DocumentIRNode Execute(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            if (irDocument.DocumentKind != null)
            {
                return irDocument;
            }

            var documentKind = ClassifyDocument(codeDocument, irDocument);
            if (documentKind == null)
            {
                return irDocument;
            }

            irDocument.DocumentKind = documentKind;

            return ExecuteCore(codeDocument, irDocument);
        }

        protected virtual DocumentIRNode ExecuteCore(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
        {
            // Rewrite a use default namespace and class declaration.
            var children = new List<RazorIRNode>(irDocument.Children);
            irDocument.Children.Clear();

            var @namespace = new NamespaceDeclarationIRNode
            {
                Content = "AspNetCore",
            };

            var @class = new ClassDeclarationIRNode
            {
                AccessModifier = "public",
                Name = GetClassName(codeDocument.Source.Filename) ?? "GeneratedClass",
                BaseType = BaseType,
            };

            var method = new RazorMethodDeclarationIRNode()
            {
                AccessModifier = "public",
                Modifiers = new List<string>() { "async", "override" },
                Name = "ExecuteAsync",
                ReturnType = "Task",
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

        protected abstract string ClassifyDocument(RazorCodeDocument codeDocument, DocumentIRNode irDocument);

        private static string GetClassName(string filename)
        {
            if (filename == null)
            {
                return null;
            }

            return SanitizeClassName("Generated_" + Path.GetFileNameWithoutExtension(filename));
        }

        // CSharp Spec §2.4.2
        private static bool IsIdentifierStart(char character)
        {
            return char.IsLetter(character) ||
                character == '_' ||
                CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.LetterNumber;
        }

        public static bool IsIdentifierPart(char character)
        {
            return char.IsDigit(character) ||
                   IsIdentifierStart(character) ||
                   IsIdentifierPartByUnicodeCategory(character);
        }

        private static bool IsIdentifierPartByUnicodeCategory(char character)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);

            return category == UnicodeCategory.NonSpacingMark || // Mn
                category == UnicodeCategory.SpacingCombiningMark || // Mc
                category == UnicodeCategory.ConnectorPunctuation || // Pc
                category == UnicodeCategory.Format; // Cf
        }

        public static string SanitizeClassName(string inputName)
        {
            if (!IsIdentifierStart(inputName[0]) && IsIdentifierPart(inputName[0]))
            {
                inputName = "_" + inputName;
            }

            var builder = new InplaceStringBuilder(inputName.Length);
            for (var i = 0; i < inputName.Length; i++)
            {
                var ch = inputName[i];
                builder.Append(IsIdentifierPart(ch) ? ch : '_');
            }

            return builder.ToString();
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

            public override void VisitDeclareTagHelperFields(DeclareTagHelperFieldsIRNode node)
            {
                _class.Insert(0, node);
            }

            public override void VisitDefault(RazorIRNode node)
            {
                _method.Add(node);
            }
        }
    }
}
