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
using System.Linq;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public struct CSharpCodeWritingScope : IDisposable
    {
        private CodeWriter _writer;
        private bool _autoSpace;
        private int _tabSize;
        private int _startIndent;

        public CSharpCodeWritingScope(CodeWriter writer) : this(writer, true) { }
        public CSharpCodeWritingScope(CodeWriter writer, int tabSize) : this(writer, tabSize, true) { }
        // TODO: Make indents (tabs) environment specific
        public CSharpCodeWritingScope(CodeWriter writer, bool autoSpace) : this(writer, 4, autoSpace) { }
        public CSharpCodeWritingScope(CodeWriter writer, int tabSize, bool autoSpace)
        {
            _writer = writer;
            _autoSpace = true;
            _tabSize = tabSize;
            _startIndent = -1; // Set in WriteStartScope

            OnClose = () => { };

            WriteStartScope();
        }

        public Action OnClose;

        public void Dispose()
        {
            WriteEndScope();
            OnClose();
        }

        private void WriteStartScope()
        {
            TryAutoSpace(" ");

            _writer.WriteLine("{").IncreaseIndent(_tabSize);
            _startIndent = _writer.CurrentIndent;
        }

        private void WriteEndScope()
        {
            TryAutoSpace(Environment.NewLine);

            // Ensure the scope hasn't been modified
            if (_writer.CurrentIndent == _startIndent)
            {
                _writer.DecreaseIndent(_tabSize);
            }

            _writer.WriteLine("}");
        }

        private void TryAutoSpace(string spaceCharacter)
        {
            if (_autoSpace && _writer.LastWrite.Length > 0 && !Char.IsWhiteSpace(_writer.LastWrite.Last()))
            {
                _writer.Write(spaceCharacter);
            }
        }
    }
}
