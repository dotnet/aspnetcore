// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Blazor.Razor
{
    // This only exists to support SourceLinesVisitor and can be removed once
    // we are able to implement proper Blazor-specific directives
    internal class SourceLinesEnumerable : IEnumerable<string>
    {
        private RazorSourceDocument _source;

        public SourceLinesEnumerable(RazorSourceDocument source)
            => _source = source;

        public IEnumerator<string> GetEnumerator()
            => new SourceLinesEnumerator(_source);

        IEnumerator IEnumerable.GetEnumerator()
            => new SourceLinesEnumerator(_source);

        private class SourceLinesEnumerator : IEnumerator<string>
        {
            private readonly RazorSourceDocument _sourceDocument;
            private readonly RazorSourceLineCollection _lines;
            private int _currentLineIndex;
            private int _cumulativeLengthOfPrecedingLines;
            private char[] _currentLineBuffer = new char[200]; // Grows if needed
            private string _currentLineText;

            public SourceLinesEnumerator(RazorSourceDocument sourceDocument)
            {
                _sourceDocument = sourceDocument ?? throw new ArgumentNullException(nameof(sourceDocument));
                _lines = _sourceDocument.Lines;
                _currentLineIndex = -1;
            }

            public string Current => _currentLineText;

            object IEnumerator.Current => _currentLineText;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                _currentLineIndex++;
                if (_currentLineIndex >= _lines.Count)
                {
                    return false;
                }

                var lineLength = _lines.GetLineLength(_currentLineIndex);
                if (_currentLineBuffer.Length < lineLength)
                {
                    _currentLineBuffer = new char[lineLength];
                }

                _sourceDocument.CopyTo(_cumulativeLengthOfPrecedingLines, _currentLineBuffer, 0, lineLength);
                _currentLineText = new string(_currentLineBuffer, 0, lineLength);
                _cumulativeLengthOfPrecedingLines += lineLength;
                return true;
            }

            public void Reset()
            {
                _currentLineIndex = -1;
                _cumulativeLengthOfPrecedingLines = 0;
            }
        }
    }
}
