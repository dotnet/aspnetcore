// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

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
