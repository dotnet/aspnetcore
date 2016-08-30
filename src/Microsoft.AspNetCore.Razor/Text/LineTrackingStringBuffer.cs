// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Razor.Parser;

namespace Microsoft.AspNetCore.Razor.Text
{
    internal class LineTrackingStringBuffer
    {
        private TextLine _currentLine;
        private TextLine _endLine;
        private IList<TextLine> _lines;

        public LineTrackingStringBuffer()
        {
            _endLine = new TextLine(0, 0);
            _lines = new List<TextLine>() { _endLine };
        }

        public int Length
        {
            get { return _endLine.End; }
        }

        public SourceLocation EndLocation
        {
            get { return new SourceLocation(Length, _lines.Count - 1, _lines[_lines.Count - 1].Length); }
        }

        public void Append(string content)
        {
            var previousIndex = 0;
            for (var i = 0; i < content.Length; i++)
            {
                // \r on it's own: Start a new line, otherwise wait for \n
                // Other Newline: Start a new line
                if ((content[i] == '\r' && (i + 1 == content.Length || content[i + 1] != '\n')) ||
                    (content[i] != '\r' && ParserHelpers.IsNewLine(content[i])))
                {
                    AppendCore(content, previousIndex, i - previousIndex + 1);
                    previousIndex = i + 1;
                    PushNewLine();
                }
            }

            if (previousIndex < content.Length)
            {
                AppendCore(content, previousIndex, content.Length - previousIndex);
            }
        }

        public CharacterReference CharAt(int absoluteIndex)
        {
            var line = FindLine(absoluteIndex);
            if (line == null)
            {
                throw new ArgumentOutOfRangeException(nameof(absoluteIndex));
            }
            var idx = absoluteIndex - line.Start;
            return new CharacterReference(line.Content[idx], new SourceLocation(absoluteIndex, line.Index, idx));
        }

        private void PushNewLine()
        {
            _endLine = new TextLine(_endLine.End, _endLine.Index + 1);
            _lines.Add(_endLine);
        }

        private void AppendCore(string content, int index, int length)
        {
            Debug.Assert(_lines.Count > 0);
            _lines[_lines.Count - 1].Content.Append(content, index, length);
        }

        private void AppendCore(char chr)
        {
            Debug.Assert(_lines.Count > 0);
            _lines[_lines.Count - 1].Content.Append(chr);
        }

        private TextLine FindLine(int absoluteIndex)
        {
            TextLine selected = null;

            if (_currentLine != null)
            {
                if (_currentLine.Contains(absoluteIndex))
                {
                    // This index is on the last read line
                    selected = _currentLine;
                }
                else if (absoluteIndex > _currentLine.Index && _currentLine.Index + 1 < _lines.Count)
                {
                    // This index is ahead of the last read line
                    selected = ScanLines(absoluteIndex, _currentLine.Index);
                }
            }

            // Have we found a line yet?
            if (selected == null)
            {
                // Scan from line 0
                selected = ScanLines(absoluteIndex, 0);
            }

            Debug.Assert(selected == null || selected.Contains(absoluteIndex));
            _currentLine = selected;
            return selected;
        }

        private TextLine ScanLines(int absoluteIndex, int startPos)
        {
            for (int i = 0; i < _lines.Count; i++)
            {
                var idx = (i + startPos) % _lines.Count;
                Debug.Assert(idx >= 0 && idx < _lines.Count);

                if (_lines[idx].Contains(absoluteIndex))
                {
                    return _lines[idx];
                }
            }
            return null;
        }

        internal struct CharacterReference
        {
            private readonly char _character;
            private readonly SourceLocation _location;

            public CharacterReference(char character, SourceLocation location)
            {
                _character = character;
                _location = location;
            }

            public char Character { get { return _character; } }
            public SourceLocation Location { get { return _location; } }
        }

        private class TextLine
        {
            private StringBuilder _content = new StringBuilder();

            public TextLine(int start, int index)
            {
                Start = start;
                Index = index;
            }

            public StringBuilder Content
            {
                get { return _content; }
            }

            public int Length
            {
                get { return Content.Length; }
            }

            public int Start { get; set; }
            public int Index { get; set; }

            public int End
            {
                get { return Start + Length; }
            }

            public bool Contains(int index)
            {
                return index < End && index >= Start;
            }
        }
    }
}
