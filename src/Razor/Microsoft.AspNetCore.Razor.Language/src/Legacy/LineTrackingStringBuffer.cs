// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class LineTrackingStringBuffer
    {
        private readonly IList<TextLine> _lines;
        private readonly string _filePath;
        private TextLine _currentLine;

        public LineTrackingStringBuffer(string content, string filePath)
            : this(content.ToCharArray(), filePath)
        {
        }

        public LineTrackingStringBuffer(char[] content, string filePath)
        {
            _lines = new List<TextLine>();

            BuildTextLines(content);

            _filePath = filePath;
        }

        public int Length
        {
            get { return _lines[_lines.Count - 1].End; }
        }

        public SourceLocation EndLocation
        {
            get { return new SourceLocation(_filePath, Length, _lines.Count - 1, _lines[_lines.Count - 1].Length); }
        }

        public CharacterReference CharAt(int absoluteIndex)
        {
            var line = FindLine(absoluteIndex);
            if (line.IsDefault)
            {
                throw new ArgumentOutOfRangeException(nameof(absoluteIndex));
            }
            var idx = absoluteIndex - line.Start;
            return new CharacterReference(line.Content[idx], new SourceLocation(_filePath, absoluteIndex, line.Index, idx));
        }

        private void BuildTextLines(char[] content)
        {
            string lineText;
            var lineStart = 0;

            for (int i = 0; i < content.Length; i++)
            {
                if (ParserHelpers.IsNewLine(content[i]))
                {
                    // \r on it's own: Start a new line, otherwise wait for \n
                    // Other Newline: Start a new line
                    if (content[i] == '\r' && i + 1 < content.Length && content[i + 1] == '\n')
                    {
                        i++;
                    }

                    lineText = new string(content, lineStart, (i - lineStart) + 1); // +1 to include the current char
                    _lines.Add(new TextLine(lineStart, _lines.Count, lineText));

                    lineStart = i + 1;
                }
            }

            lineText = new string(content, lineStart, content.Length - lineStart); // no +1 as content.Length points past the last char already
            _lines.Add(new TextLine(lineStart, _lines.Count, lineText));
        }

        private TextLine FindLine(int absoluteIndex)
        {
            TextLine selected;

            if (_currentLine.IsDefault)
            {
                // Scan from line 0
                selected = ScanLines(absoluteIndex, 0, _lines.Count);
            }
            else if (absoluteIndex >= _currentLine.End)
            {
                if (_currentLine.Index + 1 < _lines.Count)
                {
                    // This index is after the last read line
                    var nextLine = _lines[_currentLine.Index + 1];

                    // Optimization to not search if it's the common case where the line after _currentLine is being requested.
                    if (nextLine.Contains(absoluteIndex))
                    {
                        selected = nextLine;
                    }
                    else
                    {
                        selected = ScanLines(absoluteIndex, _currentLine.Index, _lines.Count);
                    }
                }
                else
                {
                    selected = default;
                }
            }
            else if (absoluteIndex < _currentLine.Start)
            {
                if (_currentLine.Index > 0)
                {
                    // This index is before the last read line
                    var prevLine = _lines[_currentLine.Index - 1];

                    // Optimization to not search if it's the common case where the line before _currentLine is being requested.
                    if (prevLine.Contains(absoluteIndex))
                    {
                        selected = prevLine;
                    }
                    else
                    {
                        selected = ScanLines(absoluteIndex, 0, _currentLine.Index);
                    }
                }
                else
                {
                    selected = default;
                }
            }
            else
            {
                // This index is on the last read line
                selected = _currentLine;
            }

            Debug.Assert(selected.IsDefault || selected.Contains(absoluteIndex));
            _currentLine = selected;
            return selected;
        }

        private TextLine ScanLines(int absoluteIndex, int startLineIndex, int endLineIndex)
        {
            // binary search for the line containing absoluteIndex
            var lowIndex = startLineIndex;
            var highIndex = endLineIndex;

            while (lowIndex != highIndex)
            {
                var midIndex = (lowIndex + highIndex) / 2;
                var midLine = _lines[midIndex];

                if (absoluteIndex >= midLine.End)
                {
                    lowIndex = midIndex + 1;
                }
                else if (absoluteIndex < midLine.Start)
                {
                    highIndex = midIndex;
                }
                else
                {
                    return midLine;
                }
            }

            return default;
        }

        internal struct CharacterReference
        {
            public CharacterReference(char character, SourceLocation location)
            {
                Character = character;
                Location = location;
            }

            public char Character { get; }

            public SourceLocation Location { get; }
        }

        private struct TextLine
        {
            public TextLine(int start, int index, string content)
            {
                Start = start;
                Index = index;
                Content = content;
            }

            public string Content { get; }

            public bool IsDefault => Content == null;

            public int Length
            {
                get { return Content.Length; }
            }

            public int Start { get; }
            public int Index { get; }

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
