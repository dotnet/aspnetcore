// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text;

namespace Microsoft.AspNet.Razor.CodeGenerators
{
    public class CodeWriter : IDisposable
    {
        private static readonly char[] NewLineCharacters = new char[] { '\r', '\n' };

        private string _cache = string.Empty;
        private bool _dirty;

        private int _absoluteIndex;
        private int _currentLineIndex;
        private int _currentLineCharacterIndex;

        public StringBuilder Builder { get; } = new StringBuilder();

        public int CurrentIndent { get; private set; }

        public bool IsAfterNewLine { get; private set; }

        public string NewLine { get; set; } = Environment.NewLine;

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

        public SourceLocation GetCurrentSourceLocation()
        {
            return new SourceLocation(_absoluteIndex, _currentLineIndex, _currentLineCharacterIndex);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Builder.Clear();
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
        }
    }
}
