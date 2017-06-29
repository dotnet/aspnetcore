// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public sealed class CodeWriter
    {
        private const string InstanceMethodFormat = "{0}.{1}";

        private static readonly char[] CStyleStringLiteralEscapeChars = {
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

        private static readonly char[] NewLineCharacters = { '\r', '\n' };

        private string _cache = string.Empty;
        private bool _dirty;

        private int _absoluteIndex;
        private int _currentLineIndex;
        private int _currentLineCharacterIndex;

        internal StringBuilder Builder { get; } = new StringBuilder();

        public int CurrentIndent { get; set; }

        public bool IsAfterNewLine { get; private set; }

        public string NewLine { get; set; } = Environment.NewLine;

        public SourceLocation Location => new SourceLocation(_absoluteIndex, _currentLineIndex, _currentLineCharacterIndex);

        // Internal for testing.
        internal CodeWriter Indent(int size)
        {
            if (IsAfterNewLine)
            {
                Builder.Append(' ', size);

                _currentLineCharacterIndex += size;
                _absoluteIndex += size;

                _dirty = true;
                IsAfterNewLine = false;
            }

            return this;
        }

        public CodeWriter Write(string data)
        {
            if (data == null)
            {
                return this;
            }

            return Write(data, 0, data.Length);
        }

        public CodeWriter Write(string data, int index, int count)
        {
            if (data == null || count == 0)
            {
                return this;
            }

            Indent(CurrentIndent);

            Builder.Append(data, index, count);

            _dirty = true;
            IsAfterNewLine = false;

            _absoluteIndex += count;

            // The data string might contain a partial newline where the previously
            // written string has part of the newline.
            var i = index;
            int? trailingPartStart = null;

            if (
                // Check the last character of the previous write operation.
                Builder.Length - count - 1 >= 0 &&
                Builder[Builder.Length - count - 1] == '\r' &&

                // Check the first character of the current write operation.
                Builder[Builder.Length - count] == '\n')
            {
                // This is newline that's spread across two writes. Skip the first character of the
                // current write operation.
                //
                // We don't need to increment our newline counter because we already did that when we
                // saw the \r.
                i += 1;
                trailingPartStart = 1;
            }

            // Iterate the string, stopping at each occurrence of a newline character. This lets us count the
            // newline occurrences and keep the index of the last one.
            while ((i = data.IndexOfAny(NewLineCharacters, i)) >= 0)
            {
                // Newline found.
                _currentLineIndex++;
                _currentLineCharacterIndex = 0;

                i++;

                // We might have stopped at a \r, so check if it's followed by \n and then advance the index to
                // start the next search after it.
                if (count > i &&
                    data[i - 1] == '\r' &&
                    data[i] == '\n')
                {
                    i++;
                }

                // The 'suffix' of the current line starts after this newline token.
                trailingPartStart = i;
            }

            if (trailingPartStart == null)
            {
                // No newlines, just add the length of the data buffer
                _currentLineCharacterIndex += count;
            }
            else
            {
                // Newlines found, add the trailing part of 'data'
                _currentLineCharacterIndex += (count - trailingPartStart.Value);
            }

            return this;
        }

        public CodeWriter WriteLine()
        {
            Builder.Append(NewLine);

            _currentLineIndex++;
            _currentLineCharacterIndex = 0;
            _absoluteIndex += NewLine.Length;

            _dirty = true;
            IsAfterNewLine = true;

            return this;
        }

        public CodeWriter WriteLine(string data)
        {
            return Write(data).WriteLine();
        }

        public string GenerateCode()
        {
            if (_dirty)
            {
                _cache = Builder.ToString();
                _dirty = false;
            }

            return _cache;
        }

        public CodeWriter WritePadding(int offset, SourceSpan? span, CodeRenderingContext context)
        {
            if (span == null)
            {
                return this;
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
                    Write("\t");
                }

                var spaces = resolvedPadding % context.Options.IndentSize;
                for (var i = 0; i < spaces; i++)
                {
                    Write(" ");
                }
            }
            else
            {
                for (var i = 0; i < resolvedPadding; i++)
                {
                    Write(" ");
                }
            }

            return this;

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

        public CodeWriter WriteVariableDeclaration(string type, string name, string value)
        {
            Write(type).Write(" ").Write(name);
            if (!string.IsNullOrEmpty(value))
            {
                Write(" = ").Write(value);
            }
            else
            {
                Write(" = null");
            }

            WriteLine(";");

            return this;
        }

        public CodeWriter WriteBooleanLiteral(bool value)
        {
            return Write(value.ToString().ToLowerInvariant());
        }

        public CodeWriter WriteStartAssignment(string name)
        {
            return Write(name).Write(" = ");
        }

        public CodeWriter WriteParameterSeparator()
        {
            return Write(", ");
        }

        public CodeWriter WriteStartNewObject(string typeName)
        {
            return Write("new ").Write(typeName).Write("(");
        }

        public CodeWriter WriteStringLiteral(string literal)
        {
            if (literal.Length >= 256 && literal.Length <= 1500 && literal.IndexOf('\0') == -1)
            {
                WriteVerbatimStringLiteral(literal);
            }
            else
            {
                WriteCStyleStringLiteral(literal);
            }

            return this;
        }

        public CodeWriter WriteUsing(string name)
        {
            return WriteUsing(name, endLine: true);
        }

        public CodeWriter WriteUsing(string name, bool endLine)
        {
            Write("using ");
            Write(name);

            if (endLine)
            {
                WriteLine(";");
            }

            return this;
        }

        /// <summary>
        /// Writes a <c>#line</c> pragma directive for the line number at the specified <paramref name="location"/>.
        /// </summary>
        /// <param name="location">The location to generate the line pragma for.</param>
        /// <param name="file">The file to generate the line pragma for.</param>
        /// <returns>The current instance of <see cref="CodeWriter"/>.</returns>
        public CodeWriter WriteLineNumberDirective(SourceSpan location, string file)
        {
            if (location.FilePath != null)
            {
                file = location.FilePath;
            }

            if (Builder.Length >= NewLine.Length && !IsAfterNewLine)
            {
                WriteLine();
            }

            var lineNumberAsString = (location.LineIndex + 1).ToString(CultureInfo.InvariantCulture);
            return Write("#line ").Write(lineNumberAsString).Write(" \"").Write(file).WriteLine("\"");
        }

        public CodeWriter WriteStartMethodInvocation(string methodName)
        {
            Write(methodName);

            return Write("(");
        }

        public CodeWriter WriteEndMethodInvocation()
        {
            return WriteEndMethodInvocation(endLine: true);
        }

        public CodeWriter WriteEndMethodInvocation(bool endLine)
        {
            Write(")");
            if (endLine)
            {
                WriteLine(";");
            }

            return this;
        }

        // Writes a method invocation for the given instance name.
        public CodeWriter WriteInstanceMethodInvocation(
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

            return WriteInstanceMethodInvocation(instanceName, methodName, endLine: true, parameters: parameters);
        }

        // Writes a method invocation for the given instance name.
        public CodeWriter WriteInstanceMethodInvocation(
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
                string.Format(CultureInfo.InvariantCulture, InstanceMethodFormat, instanceName, methodName),
                endLine,
                parameters);
        }

        public CodeWriter WriteStartInstanceMethodInvocation(string instanceName, string methodName)
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
                string.Format(CultureInfo.InvariantCulture, InstanceMethodFormat, instanceName, methodName));
        }

        public CodeWriter WriteField(IList<string> modifiers, string typeName, string fieldName)
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
                Write(modifiers[i]);
                Write(" ");
            }

            Write(typeName);
            Write(" ");
            Write(fieldName);
            Write(";");
            WriteLine();

            return this;
        }

        public CodeWriter WriteMethodInvocation(string methodName, params string[] parameters)
        {
            return WriteMethodInvocation(methodName, endLine: true, parameters: parameters);
        }

        public CodeWriter WriteMethodInvocation(string methodName, bool endLine, params string[] parameters)
        {
            return WriteStartMethodInvocation(methodName)
                .Write(string.Join(", ", parameters))
                .WriteEndMethodInvocation(endLine);
        }

        public CodeWriter WriteAutoPropertyDeclaration(IList<string> modifiers, string typeName, string propertyName)
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
                Write(modifiers[i]);
                Write(" ");
            }

            Write(typeName);
            Write(" ");
            Write(propertyName);
            Write(" { get; set; }");
            WriteLine();

            return this;
        }

        public CSharpCodeWritingScope BuildScope()
        {
            return new CSharpCodeWritingScope(this);
        }

        public CSharpCodeWritingScope BuildLambda(params string[] parameterNames)
        {
            return BuildLambda(async: false, parameterNames: parameterNames);
        }

        public CSharpCodeWritingScope BuildAsyncLambda(params string[] parameterNames)
        {
            return BuildLambda(async: true, parameterNames: parameterNames);
        }

        private CSharpCodeWritingScope BuildLambda(bool async, string[] parameterNames)
        {
            if (async)
            {
                Write("async");
            }

            Write("(").Write(string.Join(", ", parameterNames)).Write(") => ");

            var scope = new CSharpCodeWritingScope(this);

            return scope;
        }

        public CSharpCodeWritingScope BuildNamespace(string name)
        {
            Write("namespace ").WriteLine(name);

            return new CSharpCodeWritingScope(this);
        }

        public CSharpCodeWritingScope BuildClassDeclaration(
            IList<string> modifiers,
            string name,
            string baseType,
            IEnumerable<string> interfaces)
        {
            for (var i = 0; i < modifiers.Count; i++)
            {
                Write(modifiers[i]);
                Write(" ");
            }

            Write("class ");
            Write(name);

            var hasBaseType = !string.IsNullOrEmpty(baseType);
            var hasInterfaces = interfaces != null && interfaces.Count() > 0;

            if (hasBaseType || hasInterfaces)
            {
                Write(" : ");

                if (hasBaseType)
                {
                    Write(baseType);

                    if (hasInterfaces)
                    {
                        WriteParameterSeparator();
                    }
                }

                if (hasInterfaces)
                {
                    Write(string.Join(", ", interfaces));
                }
            }

            WriteLine();

            return new CSharpCodeWritingScope(this);
        }

        public CSharpCodeWritingScope BuildMethodDeclaration(
            string accessibility,
            string returnType,
            string name,
            IEnumerable<KeyValuePair<string, string>> parameters)
        {
            Write(accessibility)
                .Write(" ")
                .Write(returnType)
                .Write(" ")
                .Write(name)
                .Write("(")
                .Write(string.Join(", ", parameters.Select(p => p.Key + " " + p.Value)))
                .WriteLine(")");

            return new CSharpCodeWritingScope(this);
        }

        public IDisposable BuildLinePragma(SourceSpan documentLocation)
        {
            if (string.IsNullOrEmpty(documentLocation.FilePath))
            {
                // Can't build a valid line pragma without a file path.
                return NullDisposable.Default;
            }

            return new LinePragmaWriter(this, documentLocation);
        }

        private void WriteVerbatimStringLiteral(string literal)
        {
            Write("@\"");

            // We need to find the index of each '"' (double-quote) to escape it.
            var start = 0;
            int end;
            while ((end = literal.IndexOf('\"', start)) > -1)
            {
                Write(literal, start, end - start);

                Write("\"\"");

                start = end + 1;
            }

            Debug.Assert(end == -1); // We've hit all of the double-quotes.

            // Write the remainder after the last double-quote.
            Write(literal, start, literal.Length - start);

            Write("\"");
        }

        private void WriteCStyleStringLiteral(string literal)
        {
            // From CSharpCodeGenerator.QuoteSnippetStringCStyle in CodeDOM
            Write("\"");

            // We need to find the index of each escapable character to escape it.
            var start = 0;
            int end;
            while ((end = literal.IndexOfAny(CStyleStringLiteralEscapeChars, start)) > -1)
            {
                Write(literal, start, end - start);

                switch (literal[end])
                {
                    case '\r':
                        Write("\\r");
                        break;
                    case '\t':
                        Write("\\t");
                        break;
                    case '\"':
                        Write("\\\"");
                        break;
                    case '\'':
                        Write("\\\'");
                        break;
                    case '\\':
                        Write("\\\\");
                        break;
                    case '\0':
                        Write("\\\0");
                        break;
                    case '\n':
                        Write("\\n");
                        break;
                    case '\u2028':
                    case '\u2029':
                        Write("\\u");
                        Write(((int)literal[end]).ToString("X4", CultureInfo.InvariantCulture));
                        break;
                    default:
                        Debug.Assert(false, "Unknown escape character.");
                        break;
                }

                start = end + 1;
            }

            Debug.Assert(end == -1); // We've hit all of chars that need escaping.

            // Write the remainder after the last escaped char.
            Write(literal, start, literal.Length - start);

            Write("\"");
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
                _autoSpace = true;
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
                    _writer.Builder.Length > 0 &&
                    !char.IsWhiteSpace(_writer.Builder[_writer.Builder.Length - 1]))
                {
                    _writer.Write(spaceCharacter);
                }
            }
        }

        private class LinePragmaWriter : IDisposable
        {
            private readonly CodeWriter _writer;
            private readonly int _startIndent;

            public LinePragmaWriter(CodeWriter writer, SourceSpan documentLocation)
            {
                if (writer == null)
                {
                    throw new ArgumentNullException(nameof(writer));
                }

                _writer = writer;
                _startIndent = _writer.CurrentIndent;
                _writer.CurrentIndent = 0;
                _writer.WriteLineNumberDirective(documentLocation, documentLocation.FilePath);
            }

            public void Dispose()
            {
                // Need to add an additional line at the end IF there wasn't one already written.
                // This is needed to work with the C# editor's handling of #line ...
                var builder = _writer.Builder;
                var endsWithNewline = builder.Length > 0 && builder[builder.Length - 1] == '\n';

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
