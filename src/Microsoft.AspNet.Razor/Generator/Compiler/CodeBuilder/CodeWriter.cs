// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeWriter : IDisposable
    {
        private static readonly char[] NewLineCharacters = new char[] { '\r', '\n' };
        private readonly StringWriter _writer = new StringWriter();
        private bool _newLine;
        private string _cache = string.Empty;
        private bool _dirty = false;

        private int _absoluteIndex;
        private int _currentLineIndex;
        private int _currentLineCharacterIndex;

        public string LastWrite { get; private set; }

        public int CurrentIndent { get; private set; }

        public string NewLine 
        { 
            get
            {
                return _writer.NewLine;
            } 
            set
            {
                _writer.NewLine = value;
            } 
        }

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

                _currentLineCharacterIndex += size;
                _absoluteIndex += size;

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

            if (data == null || data.Length == 0)
            {
                return this;
            }

            _absoluteIndex += data.Length;

            // The data string might contain a partial newline where the previously
            // written string has part of the newline.
            var i = 0;
            int? trailingPartStart = null;
            var builder = _writer.GetStringBuilder();

            if (
                // Check the last character of the previous write operation.
                builder.Length - data.Length - 1 >= 0 &&
                builder[builder.Length - data.Length - 1] == '\r' &&

                // Check the first character of the current write operation.
                builder[builder.Length - data.Length] == '\n')
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
                if (data.Length > i &&
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
                _currentLineCharacterIndex += data.Length;
            }
            else
            {
                // Newlines found, add the trailing part of 'data'
                _currentLineCharacterIndex += (data.Length - trailingPartStart.Value);
            }

            return this;
        }

        public CodeWriter WriteLine()
        {
            LastWrite = _writer.NewLine;

            _writer.WriteLine();
            Flush();

            _currentLineIndex++;
            _currentLineCharacterIndex = 0;
            _absoluteIndex += _writer.NewLine.Length;

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
            return new SourceLocation(_absoluteIndex, _currentLineIndex, _currentLineCharacterIndex);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
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
