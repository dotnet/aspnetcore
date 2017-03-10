// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorSourceLineCollection : RazorSourceLineCollection
    {
        private readonly RazorSourceDocument _document;
        private readonly int[] _lineStarts;

        public DefaultRazorSourceLineCollection(RazorSourceDocument document)
        {
            _document = document;
            _lineStarts = GetLineStarts();
        }

        public override int Count => _lineStarts.Length;

        public override int GetLineLength(int index)
        {
            if (index < 0 || index >= _lineStarts.Length)
            {
                throw new IndexOutOfRangeException(nameof(index));
            }

            if (index == _lineStarts.Length - 1)
            {
                // Last line is special.
                return _document.Length - _lineStarts[index];
            }

            return _lineStarts[index + 1] - _lineStarts[index];
        }

        internal override SourceLocation GetLocation(int position)
        {
            if (position < 0 || position >= _document.Length)
            {
                throw new IndexOutOfRangeException(nameof(position));
            }

            var index = Array.BinarySearch<int>(_lineStarts, position);
            if (index >= 0)
            {
                // We have an exact match for the start of a line.
                Debug.Assert(_lineStarts[index] == position);

                return new SourceLocation(_document.FileName, position, index, characterIndex: 0);
            }


            // Index is the complement of the line *after* the one we want, because BinarySearch tells
            // us where we'd put position *if* it were the start of a line.
            index = (~index) - 1;
            if (index == -1)
            {
                // There's no preceding line, so it's based on the start of the string
                return new SourceLocation(_document.FileName, position, 0, position);
            }
            else
            {
                var characterIndex = position - _lineStarts[index];
                return new SourceLocation(_document.FileName, position, index, characterIndex);
            }
        }

        private int[] GetLineStarts()
        {
            var starts = new List<int>();

            // We always consider a document to have at least a 0th line, even if it's empty.
            starts.Add(0);

            var unprocessedCR = false;

            // Length - 1 because we don't care if there was a linebreak as the last character.
            var length = _document.Length;
            for (var i = 0; i < length - 1; i++)
            {
                var c = _document[i];
                var isLineBreak = false;

                switch (c)
                {
                    case '\r':
                        unprocessedCR = true;
                        continue;

                    case '\n':
                        unprocessedCR = false;
                        isLineBreak = true;
                        break;

                    case '\u0085':
                    case '\u2028':
                    case '\u2029':
                        isLineBreak = true;
                        break;

                }

                if (unprocessedCR)
                {
                    // If we get here it means that we had a CR followed by something other than an LF.
                    // Add the CR as a line break.
                    starts.Add(i);
                    unprocessedCR = false;
                }

                if (isLineBreak)
                {
                    starts.Add(i + 1);
                }
            }

            return starts.ToArray();
        }
    }
}
