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

            if (context.SourceDocument.FilePath != null &&
                !string.Equals(context.SourceDocument.FilePath, span.Value.FilePath, StringComparison.OrdinalIgnoreCase))
            {
                // We don't want to generate padding for nodes from imports.
                return writer;
            }

            var basePadding = CalculatePadding();
            var resolvedPadding = Math.Max(basePadding - offset, 0);

            writer.Indent(resolvedPadding);

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
                        spaceCount += writer.TabSize;
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

        public static CodeWriter WriteEnhancedLineNumberDirective(this CodeWriter writer, SourceSpan span, int characterOffset)
        {
            // All values here need to be offset by 1 since #line uses a 1-indexed numbering system.
            var lineNumberAsString = (span.LineIndex + 1).ToString(CultureInfo.InvariantCulture);
            var characterStartAsString = (span.CharacterIndex + 1).ToString(CultureInfo.InvariantCulture);
            var lineEndAsString = (span.LineIndex + 1 + span.LineCount).ToString(CultureInfo.InvariantCulture);
            var characterEndAsString = (span.EndCharacterIndex + 1).ToString(CultureInfo.InvariantCulture);
            var characterOffsetAsString = characterOffset.ToString(CultureInfo.InvariantCulture);
            return writer.Write("#line (")
                .Write(lineNumberAsString)
                .Write(",")
                .Write(characterStartAsString)
                .Write(")-(")
                .Write(lineEndAsString)
                .Write(",")
                .Write(characterEndAsString)
                .Write(") ")
                .Write(characterOffsetAsString)
                .Write(" \"").Write(span.FilePath).WriteLine("\"");
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

        public static CodeWriter WriteField(this CodeWriter writer, IList<string> suppressWarnings, IList<string> modifiers, string typeName, string fieldName)
        {
            if (suppressWarnings == null)
            {
                throw new ArgumentNullException(nameof(suppressWarnings));
            }

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

            for (var i = 0; i < suppressWarnings.Count; i++)
            {
                writer.Write("#pragma warning disable ");
                writer.WriteLine(suppressWarnings[i]);
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

            for (var i = suppressWarnings.Count - 1; i >= 0; i--)
            {
                writer.Write("#pragma warning restore ");
                writer.WriteLine(suppressWarnings[i]);
            }

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
            IList<string> interfaces,
            IList<(string name, string constraint)> typeParameters)
        {
            for (var i = 0; i < modifiers.Count; i++)
            {
                writer.Write(modifiers[i]);
                writer.Write(" ");
            }

            writer.Write("class ");
            writer.Write(name);

            if (typeParameters != null && typeParameters.Count > 0)
            {
                writer.Write("<");
                writer.Write(string.Join(", ", typeParameters.Select(tp => tp.name)));
                writer.Write(">");
            }

            var hasBaseType = !string.IsNullOrEmpty(baseType);
            var hasInterfaces = interfaces != null && interfaces.Count > 0;

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
            if (typeParameters != null)
            {
                for (var i = 0; i < typeParameters.Count; i++)
                {
                    var constraint = typeParameters[i].constraint;
                    if (constraint != null)
                    {
                        writer.Write(constraint);
                        writer.WriteLine();
                    }
                }
            }

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

        public static IDisposable BuildLinePragma(this CodeWriter writer, SourceSpan? span, CodeRenderingContext context)
        {
            if (string.IsNullOrEmpty(span?.FilePath))
            {
                // Can't build a valid line pragma without a file path.
                return NullDisposable.Default;
            }

            return new LinePragmaWriter(writer, span.Value, context, 0, false);
        }

        public static IDisposable BuildEnhancedLinePragma(this CodeWriter writer, SourceSpan? span, CodeRenderingContext context, int characterOffset = 0)
        {
            if (string.IsNullOrEmpty(span?.FilePath))
            {
                // Can't build a valid line pragma without a file path.
                return NullDisposable.Default;
            }

            return new LinePragmaWriter(writer, span.Value, context, characterOffset, useEnhancedLinePragma: true);
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

            public CSharpCodeWritingScope(CodeWriter writer, bool autoSpace = true)
            {
                _writer = writer;
                _autoSpace = autoSpace;
                _tabSize = writer.TabSize;
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
            private readonly CodeRenderingContext _context;
            private readonly int _startIndent;
            private readonly int _sourceLineIndex;
            private readonly int _startLineIndex;
            private readonly string _sourceFilePath;

            public LinePragmaWriter(
                CodeWriter writer,
                SourceSpan span,
                CodeRenderingContext context,
                int characterOffset,
                bool useEnhancedLinePragma = false)
            {
                if (writer == null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }

                _writer = writer;
                _context = context;
                _startIndent = _writer.CurrentIndent;
                _sourceFilePath = span.FilePath;
                _sourceLineIndex = span.LineIndex;
                _writer.CurrentIndent = 0;

                if (!_context.Options.SuppressNullabilityEnforcement)
                {
                    var endsWithNewline = _writer.Length > 0 && _writer[_writer.Length - 1] == '\n';
                    if (!endsWithNewline)
                    {
                        _writer.WriteLine();
                    }
                    _writer.WriteLine("#nullable restore");
                }

                if (useEnhancedLinePragma && _context.Options.UseEnhancedLinePragma)
                {
                    WriteEnhancedLineNumberDirective(writer, span, characterOffset);
                }
                else
                {
                    WriteLineNumberDirective(writer, span);
                }
                

                // Capture the line index after writing the #line directive.
                _startLineIndex = writer.Location.LineIndex;
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

                var lineCount = _writer.Location.LineIndex - _startLineIndex;
                var linePragma = new LinePragma(_sourceLineIndex, lineCount, _sourceFilePath);
                _context.AddLinePragma(linePragma);

                _writer
                    .WriteLine("#line default")
                    .WriteLine("#line hidden");

                if (!_context.Options.SuppressNullabilityEnforcement)
                {
                    _writer.WriteLine("#nullable disable");
                }

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
