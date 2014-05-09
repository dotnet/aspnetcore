// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeWriter : IDisposable
    {
        private StringWriter _writer = new StringWriter();
        private bool _newLine;
        private string _cache = string.Empty;
        private bool _dirty = false;

        public string LastWrite { get; private set; }
        public int CurrentIndent { get; private set; }

        public CodeWriter ResetIndent()
        {
            return SetIndent(0);
        }

        public CodeWriter IncreaseIndent(int size)
        {
            CurrentIndent += size;

            return this;
        }

        public CodeWriter DecreaseIndent(int size)
        {
            CurrentIndent -= size;

            return this;
        }

        public CodeWriter SetIndent(int size)
        {
            CurrentIndent = size;

            return this;
        }

        public CodeWriter Indent(int size)
        {
            if (_newLine)
            {
                _writer.Write(new string(' ', size));
                Flush();
                _dirty = true;
                _newLine = false;
            }

            return this;
        }

        public CodeWriter Write(string data)
        {
            Indent(CurrentIndent);

            _writer.Write(data);

            Flush();

            LastWrite = data;
            _dirty = true;
            _newLine = false;

            return this;
        }

        public CodeWriter WriteLine()
        {
            LastWrite = Environment.NewLine;

            _writer.WriteLine();

            Flush();

            _dirty = true;
            _newLine = true;

            return this;
        }

        public CodeWriter WriteLine(string data)
        {
            return Write(data).WriteLine();
        }

        public CodeWriter Flush()
        {
            _writer.Flush();

            return this;
        }

        public string GenerateCode()
        {
            if (_dirty)
            {
                _cache = _writer.ToString();
                _dirty = false;
            }

            return _cache;
        }

        public SourceLocation GetCurrentSourceLocation()
        {
            string output = GenerateCode();
            string unescapedOutput = output.Replace("\\r", String.Empty).Replace("\\n", String.Empty);

            return new SourceLocation(
                absoluteIndex: output.Length,
                lineIndex: (unescapedOutput.Length - unescapedOutput.Replace(Environment.NewLine, String.Empty).Length) / Environment.NewLine.Length,
                characterIndex: unescapedOutput.Length - (unescapedOutput.LastIndexOf(Environment.NewLine) + Environment.NewLine.Length));
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {
                _writer.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }
}
