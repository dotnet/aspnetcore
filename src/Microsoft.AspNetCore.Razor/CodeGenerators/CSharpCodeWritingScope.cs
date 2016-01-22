// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;

namespace Microsoft.AspNet.Razor.CodeGenerators
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
            TryAutoSpace(_writer.NewLine);

            // Ensure the scope hasn't been modified
            if (_writer.CurrentIndent == _startIndent)
            {
                _writer.DecreaseIndent(_tabSize);
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
}
