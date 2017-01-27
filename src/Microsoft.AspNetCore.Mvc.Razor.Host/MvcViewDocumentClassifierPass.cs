// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Razor.Evolution;
using Microsoft.AspNetCore.Razor.Evolution.Intermediate;

namespace Microsoft.AspNetCore.Mvc.Razor.Host
{
    public class MvcViewDocumentClassifierPass : IRazorIRPass
    {
        public static readonly string Kind = "mvc.1.0.view";

        public RazorEngine Engine { get; set; }

        // We want to run before the default, but after others since this is the MVC default.
        public virtual int Order => RazorIRPass.DefaultDocumentClassifierOrder - 1;

        public static string DocumentKind = "default";

        public DocumentIRNode Execute(RazorCodeDocument codeDocument, DocumentIRNode irDocument)
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
                Content = "AspNetCore",
            };

            var @class = new ClassDeclarationIRNode()
            {
                AccessModifier = "public",
                Name = GetClassName(codeDocument.Source.Filename) ?? "GeneratedClass",
                BaseType = "Microsoft.AspNetCore.Mvc.Razor.RazorPage<TModel>"
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

        private static string GetClassName(string filename)
        {
            if (filename == null)
            {
                return null;
            }

            return ParserHelpers.SanitizeClassName("Generated_" + Path.GetFileNameWithoutExtension(filename));
        }

        private static class ParserHelpers
        {
            public static bool IsNewLine(char value)
            {
                return value == '\r' // Carriage return
                       || value == '\n' // Linefeed
                       || value == '\u0085' // Next Line
                       || value == '\u2028' // Line separator
                       || value == '\u2029'; // Paragraph separator
            }

            public static bool IsNewLine(string value)
            {
                return (value.Length == 1 && (IsNewLine(value[0]))) ||
                       (string.Equals(value, Environment.NewLine, StringComparison.Ordinal));
            }

            // Returns true if the character is Whitespace and NOT a newline
            public static bool IsWhitespace(char value)
            {
                return value == ' ' ||
                       value == '\f' ||
                       value == '\t' ||
                       value == '\u000B' || // Vertical Tab
                       CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.SpaceSeparator;
            }

            public static bool IsWhitespaceOrNewLine(char value)
            {
                return IsWhitespace(value) || IsNewLine(value);
            }

            public static bool IsIdentifier(string value)
            {
                return IsIdentifier(value, requireIdentifierStart: true);
            }

            public static bool IsIdentifier(string value, bool requireIdentifierStart)
            {
                IEnumerable<char> identifierPart = value;
                if (requireIdentifierStart)
                {
                    identifierPart = identifierPart.Skip(1);
                }
                return (!requireIdentifierStart || IsIdentifierStart(value[0])) && identifierPart.All(IsIdentifierPart);
            }

            public static bool IsHexDigit(char value)
            {
                return (value >= '0' && value <= '9') || (value >= 'A' && value <= 'F') || (value >= 'a' && value <= 'f');
            }

            public static bool IsIdentifierStart(char value)
            {
                return value == '_' || IsLetter(value);
            }

            public static bool IsIdentifierPart(char value)
            {
                return IsLetter(value)
                       || IsDecimalDigit(value)
                       || IsConnecting(value)
                       || IsCombining(value)
                       || IsFormatting(value);
            }

            public static bool IsTerminatingCharToken(char value)
            {
                return IsNewLine(value) || value == '\'';
            }

            public static bool IsTerminatingQuotedStringToken(char value)
            {
                return IsNewLine(value) || value == '"';
            }

            public static bool IsDecimalDigit(char value)
            {
                return CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.DecimalDigitNumber;
            }

            public static bool IsLetterOrDecimalDigit(char value)
            {
                return IsLetter(value) || IsDecimalDigit(value);
            }

            public static bool IsLetter(char value)
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(value);

                return cat == UnicodeCategory.UppercaseLetter
                       || cat == UnicodeCategory.LowercaseLetter
                       || cat == UnicodeCategory.TitlecaseLetter
                       || cat == UnicodeCategory.ModifierLetter
                       || cat == UnicodeCategory.OtherLetter
                       || cat == UnicodeCategory.LetterNumber;
            }

            public static bool IsFormatting(char value)
            {
                return CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.Format;
            }

            public static bool IsCombining(char value)
            {
                UnicodeCategory cat = CharUnicodeInfo.GetUnicodeCategory(value);

                return cat == UnicodeCategory.SpacingCombiningMark || cat == UnicodeCategory.NonSpacingMark;

            }

            public static bool IsConnecting(char value)
            {
                return CharUnicodeInfo.GetUnicodeCategory(value) == UnicodeCategory.ConnectorPunctuation;
            }

            public static string SanitizeClassName(string inputName)
            {
                if (!IsIdentifierStart(inputName[0]) && IsIdentifierPart(inputName[0]))
                {
                    inputName = "_" + inputName;
                }

                return new String((from value in inputName
                                   select IsIdentifierPart(value) ? value : '_')
                                      .ToArray());
            }

            public static bool IsEmailPart(char character)
            {
                // Source: http://tools.ietf.org/html/rfc5322#section-3.4.1
                // We restrict the allowed characters to alpha-numerics and '_' in order to ensure we cover most of the cases where an
                // email address is intended without restricting the usage of code within JavaScript, CSS, and other contexts.
                return Char.IsLetter(character) || Char.IsDigit(character) || character == '_';
            }
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
