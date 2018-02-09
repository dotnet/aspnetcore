// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal static class CodeWriterExtensions
    {
        private const string InstanceMethodFormat = "{0}.{1}";

        private static readonly char[] CStyleStringLiteralEscapeChars =
        {
            '\r',
            '\t',
            '\"',
            '\'',
            '\\',
            '\0',
            '\n',
            '\u2028',
            '\u2029',
        };

        public static bool IsAtBeginningOfLine(this CodeWriter writer)
        {
            return writer.Length == 0 || writer[writer.Length - 1] == '\n';
        }

        public static CodeWriter WritePadding(this CodeWriter writer, int offset, SourceSpan? span, CodeRenderingContext context)
        {
            if (span == null)
            {
                return writer;
            }

            var basePadding = CalculatePadding();
            var resolvedPadding = Math.Max(basePadding - offset, 0);

            if (context.Options.IndentWithTabs)
            {
                // Avoid writing directly to the StringBuilder here, that will throw off the manual indexing 
                // done by the base class.
                var tabs = resolvedPadding / context.Options.IndentSize;
                for (var i = 0; i < tabs; i++)
                {
                    writer.Write("\t");
                }

                var spaces = resolvedPadding % context.Options.IndentSize;
                for (var i = 0; i < spaces; i++)
                {
                    writer.Write(" ");
                }
            }
            else
            {
                for (var i = 0; i < resolvedPadding; i++)
                {
                    writer.Write(" ");
                }
            }

            return writer;

            int CalculatePadding()
            {
                var spaceCount = 0;
                for (var i = span.Value.AbsoluteIndex - 1; i >= 0; i--)
                {
                    var @char = context.SourceDocument[i];
                    if (@char == '\n' || @char == '\r')
                    {
                        break;
                    }
                    else if (@char == '\t')
                    {
                        spaceCount += context.Options.IndentSize;
                    }
                    else
                    {
                        spaceCount++;
                    }
                }

                return spaceCount;
            }
        }

        public static CodeWriter WriteVariableDeclaration(this CodeWriter writer, string type, string name, string value)
        {
            writer.Write(type).Write(" ").Write(name);
            if (!string.IsNullOrEmpty(value))
            {
                writer.Write(" = ").Write(value);
            }
            else
            {
                writer.Write(" = null");
            }

            writer.WriteLine(";");

            return writer;
        }

        public static CodeWriter WriteBooleanLiteral(this CodeWriter writer, bool value)
        {
            return writer.Write(value.ToString().ToLowerInvariant());
        }

        public static CodeWriter WriteStartAssignment(this CodeWriter writer, string name)
        {
            return writer.Write(name).Write(" = ");
        }

        public static CodeWriter WriteParameterSeparator(this CodeWriter writer)
        {
            return writer.Write(", ");
        }

        public static CodeWriter WriteStartNewObject(this CodeWriter writer, string typeName)
        {
            return writer.Write("new ").Write(typeName).Write("(");
        }

        public static CodeWriter WriteStringLiteral(this CodeWriter writer, string literal)
        {
            if (literal.Length >= 256 && literal.Length <= 1500 && literal.IndexOf('\0') == -1)
            {
                WriteVerbatimStringLiteral(writer, literal);
            }
            else
            {
                WriteCStyleStringLiteral(writer, literal);
            }

            return writer;
        }

        public static CodeWriter WriteUsing(this CodeWriter writer, string name)
        {
            return WriteUsing(writer, name, endLine: true);
        }

        public static CodeWriter WriteUsing(this CodeWriter writer, string name, bool endLine)
        {
            writer.Write("using ");
            writer.Write(name);

            if (endLine)
            {
                writer.WriteLine(";");
            }

            return writer;
        }

        public static CodeWriter WriteLineNumberDirective(this CodeWriter writer, SourceSpan span)
        {
            if (writer.Length >= writer.NewLine.Length && !IsAtBeginningOfLine(writer))
            {
                writer.WriteLine();
            }

            var lineNumberAsString = (span.LineIndex + 1).ToString(CultureInfo.InvariantCulture);
            return writer.Write("#line ").Write(lineNumberAsString).Write(" \"").Write(span.FilePath).WriteLine("\"");
        }

        public static CodeWriter WriteStartMethodInvocation(this CodeWriter writer, string methodName)
        {
            writer.Write(methodName);

            return writer.Write("(");
        }

        public static CodeWriter WriteEndMethodInvocation(this CodeWriter writer)
        {
            return WriteEndMethodInvocation(writer, endLine: true);
        }

        public static CodeWriter WriteEndMethodInvocation(this CodeWriter writer, bool endLine)
        {
            writer.Write(")");
            if (endLine)
            {
                writer.WriteLine(";");
            }

            return writer;
        }

        // Writes a method invocation for the given instance name.
        public static CodeWriter WriteInstanceMethodInvocation(
            this CodeWriter writer,
            string instanceName,
            string methodName,
            params string[] parameters)
        {
            if (instanceName == null)
            {
                throw new ArgumentNullException(nameof(instanceName));
            }

            if (methodName == null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            return WriteInstanceMethodInvocation(writer, instanceName, methodName, endLine: true, parameters: parameters);
        }

        // Writes a method invocation for the given instance name.
        public static CodeWriter WriteInstanceMethodInvocation(
            this CodeWriter writer,
            string instanceName,
            string methodName,
            bool endLine,
            params string[] parameters)
        {
            if (instanceName == null)
            {
                throw new ArgumentNullException(nameof(instanceName));
            }

            if (methodName == null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            return WriteMethodInvocation(
                writer,
                string.Format(CultureInfo.InvariantCulture, InstanceMethodFormat, instanceName, methodName),
                endLine,
                parameters);
        }

        public static CodeWriter WriteStartInstanceMethodInvocation(this CodeWriter writer, string instanceName, string methodName)
        {
            if (instanceName == null)
            {
                throw new ArgumentNullException(nameof(instanceName));
            }

            if (methodName == null)
            {
                throw new ArgumentNullException(nameof(methodName));
            }

            return WriteStartMethodInvocation(
                writer,
                string.Format(CultureInfo.InvariantCulture, InstanceMethodFormat, instanceName, methodName));
        }

        public static CodeWriter WriteField(this CodeWriter writer, IList<string> modifiers, string typeName, string fieldName)
        {
            if (modifiers == null)
            {
                throw new ArgumentNullException(nameof(modifiers));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            for (var i = 0; i < modifiers.Count; i++)
            {
                writer.Write(modifiers[i]);
                writer.Write(" ");
            }

            writer.Write(typeName);
            writer.Write(" ");
            writer.Write(fieldName);
            writer.Write(";");
            writer.WriteLine();

            return writer;
        }

        public static CodeWriter WriteMethodInvocation(this CodeWriter writer, string methodName, params string[] parameters)
        {
            return WriteMethodInvocation(writer, methodName, endLine: true, parameters: parameters);
        }

        public static CodeWriter WriteMethodInvocation(this CodeWriter writer, string methodName, bool endLine, params string[] parameters)
        {
            return 
                WriteStartMethodInvocation(writer, methodName)
                .Write(string.Join(", ", parameters))
                .WriteEndMethodInvocation(endLine);
        }

        public static CodeWriter WriteAutoPropertyDeclaration(this CodeWriter writer, IList<string> modifiers, string typeName, string propertyName)
        {
            if (modifiers == null)
            {
                throw new ArgumentNullException(nameof(modifiers));
            }

            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            for (var i = 0; i < modifiers.Count; i++)
            {
                writer.Write(modifiers[i]);
                writer.Write(" ");
            }

            writer.Write(typeName);
            writer.Write(" ");
            writer.Write(propertyName);
            writer.Write(" { get; set; }");
            writer.WriteLine();

            return writer;
        }

        public static CSharpCodeWritingScope BuildScope(this CodeWriter writer)
        {
            return new CSharpCodeWritingScope(writer);
        }

        public static CSharpCodeWritingScope BuildLambda(this CodeWriter writer, params string[] parameterNames)
        {
            return BuildLambda(writer, async: false, parameterNames: parameterNames);
        }

        public static CSharpCodeWritingScope BuildAsyncLambda(this CodeWriter writer, params string[] parameterNames)
        {
            return BuildLambda(writer, async: true, parameterNames: parameterNames);
        }

        private static CSharpCodeWritingScope BuildLambda(CodeWriter writer, bool async, string[] parameterNames)
        {
            if (async)
            {
                writer.Write("async");
            }

            writer.Write("(").Write(string.Join(", ", parameterNames)).Write(") => ");

            var scope = new CSharpCodeWritingScope(writer);

            return scope;
        }

        public static CSharpCodeWritingScope BuildNamespace(this CodeWriter writer, string name)
        {
            writer.Write("namespace ").WriteLine(name);

            return new CSharpCodeWritingScope(writer);
        }

        public static CSharpCodeWritingScope BuildClassDeclaration(
            this CodeWriter writer,
            IList<string> modifiers,
            string name,
            string baseType,
            IEnumerable<string> interfaces)
        {
            for (var i = 0; i < modifiers.Count; i++)
            {
                writer.Write(modifiers[i]);
                writer.Write(" ");
            }

            writer.Write("class ");
            writer.Write(name);

            var hasBaseType = !string.IsNullOrEmpty(baseType);
            var hasInterfaces = interfaces != null && interfaces.Count() > 0;

            if (hasBaseType || hasInterfaces)
            {
                writer.Write(" : ");

                if (hasBaseType)
                {
                    writer.Write(baseType);

                    if (hasInterfaces)
                    {
                        WriteParameterSeparator(writer);
                    }
                }

                if (hasInterfaces)
                {
                    writer.Write(string.Join(", ", interfaces));
                }
            }

            writer.WriteLine();

            return new CSharpCodeWritingScope(writer);
        }

        public static CSharpCodeWritingScope BuildMethodDeclaration(
            this CodeWriter writer,
            string accessibility,
            string returnType,
            string name,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            writer.Write(accessibility)
                .Write(" ")
                .Write(returnType)
                .Write(" ")
                .Write(name)
                .Write("(")
                .Write(string.Join(", ", parameters.Select(p => p.Key + " " + p.Value)))
                .WriteLine(")");

            return new CSharpCodeWritingScope(writer);
        }

        public static IDisposable BuildLinePragma(this CodeWriter writer, SourceSpan? span)
        {
            if (string.IsNullOrEmpty(span?.FilePath))
            {
                // Can't build a valid line pragma without a file path.
                return NullDisposable.Default;
            }

            return new LinePragmaWriter(writer, span.Value);
        }

        private static void WriteVerbatimStringLiteral(CodeWriter writer, string literal)
        {
            writer.Write("@\"");

            // We need to suppress indenting during the writing of the string's content. A
            // verbatim string literal could contain newlines that don't get escaped.
            var indent = writer.CurrentIndent;
            writer.CurrentIndent = 0;

            // We need to find the index of each '"' (double-quote) to escape it.
            var start = 0;
            int end;
            while ((end = literal.IndexOf('\"', start)) > -1)
            {
                writer.Write(literal, start, end - start);

                writer.Write("\"\"");

                start = end + 1;
            }

            Debug.Assert(end == -1); // We've hit all of the double-quotes.

            // Write the remainder after the last double-quote.
            writer.Write(literal, start, literal.Length - start);

            writer.Write("\"");

            writer.CurrentIndent = indent;
        }

        private static void WriteCStyleStringLiteral(CodeWriter writer, string literal)
        {
            // From CSharpCodeGenerator.QuoteSnippetStringCStyle in CodeDOM
            writer.Write("\"");

            // We need to find the index of each escapable character to escape it.
            var start = 0;
            int end;
            while ((end = literal.IndexOfAny(CStyleStringLiteralEscapeChars, start)) > -1)
            {
                writer.Write(literal, start, end - start);

                switch (literal[end])
                {
                    case '\r':
                        writer.Write("\\r");
                        break;
                    case '\t':
                        writer.Write("\\t");
                        break;
                    case '\"':
                        writer.Write("\\\"");
                        break;
                    case '\'':
                        writer.Write("\\\'");
                        break;
                    case '\\':
                        writer.Write("\\\\");
                        break;
                    case '\0':
                        writer.Write("\\\0");
                        break;
                    case '\n':
                        writer.Write("\\n");
                        break;
                    case '\u2028':
                    case '\u2029':
                        writer.Write("\\u");
                        writer.Write(((int)literal[end]).ToString("X4", CultureInfo.InvariantCulture));
                        break;
                    default:
                        Debug.Assert(false, "Unknown escape character.");
                        break;
                }

                start = end + 1;
            }

            Debug.Assert(end == -1); // We've hit all of chars that need escaping.

            // Write the remainder after the last escaped char.
            writer.Write(literal, start, literal.Length - start);

            writer.Write("\"");
        }

        public struct CSharpCodeWritingScope : IDisposable
        {
            private CodeWriter _writer;
            private bool _autoSpace;
            private int _tabSize;
            private int _startIndent;

            public CSharpCodeWritingScope(CodeWriter writer, int tabSize = 4, bool autoSpace = true)
            {
                _writer = writer;
                _autoSpace = autoSpace;
                _tabSize = tabSize;
                _startIndent = -1; // Set in WriteStartScope

                WriteStartScope();
            }

            public void Dispose()
            {
                WriteEndScope();
            }

            private void WriteStartScope()
            {
                TryAutoSpace(" ");

                _writer.WriteLine("{");
                _writer.CurrentIndent += _tabSize;
                _startIndent = _writer.CurrentIndent;
            }

            private void WriteEndScope()
            {
                TryAutoSpace(_writer.NewLine);

                // Ensure the scope hasn't been modified
                if (_writer.CurrentIndent == _startIndent)
                {
                    _writer.CurrentIndent -= _tabSize;
                }

                _writer.WriteLine("}");
            }

            private void TryAutoSpace(string spaceCharacter)
            {
                if (_autoSpace &&
                    _writer.Length > 0 &&
                    !char.IsWhiteSpace(_writer[_writer.Length - 1]))
                {
                    _writer.Write(spaceCharacter);
                }
            }
        }

        private class LinePragmaWriter : IDisposable
        {
            private readonly CodeWriter _writer;
            private readonly int _startIndent;

            public LinePragmaWriter(CodeWriter writer, SourceSpan span)
            {
                if (writer == null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }

                _writer = writer;
                _startIndent = _writer.CurrentIndent;
                _writer.CurrentIndent = 0;
                WriteLineNumberDirective(writer, span);
            }

            public void Dispose()
            {
                // Need to add an additional line at the end IF there wasn't one already written.
                // This is needed to work with the C# editor's handling of #line ...
                var endsWithNewline = _writer.Length > 0 && _writer[_writer.Length - 1] == '\n';

                // Always write at least 1 empty line to potentially separate code from pragmas.
                _writer.WriteLine();

                // Check if the previous empty line wasn't enough to separate code from pragmas.
                if (!endsWithNewline)
                {
                    _writer.WriteLine();
                }

                _writer
                    .WriteLine("#line default")
                    .WriteLine("#line hidden");

                _writer.CurrentIndent = _startIndent;
            }
        }

        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Default = new NullDisposable();

            private NullDisposable()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
