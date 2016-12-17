// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.AspNetCore.Razor.Evolution.Legacy;

namespace Microsoft.AspNetCore.Razor.Evolution
{
    internal class DefaultRazorSourceDocument : RazorSourceDocument
    {
        private readonly string _content;
        private readonly RazorSourceLineCollection _lines;

        public DefaultRazorSourceDocument(string content, Encoding encoding, string filename)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (encoding == null)
            {
                throw new ArgumentNullException(nameof(encoding));
            }

            _content = content;
            Encoding = encoding;
            Filename = filename;

            _lines = new LineCollection(this, LineCollection.GetLineStarts(content));
        }

        public override char this[int position] => _content[position];

        public override Encoding Encoding { get; }

        public override string Filename { get; }

        public override int Length => _content.Length;

        public override RazorSourceLineCollection Lines => _lines;

        public override void CopyTo(int sourceIndex, char[] destination, int destinationIndex, int count)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            if (sourceIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex));
            }

            if (destinationIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(destinationIndex));
            }

            if (count < 0 || count > Length - sourceIndex || count > destination.Length - destinationIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (count == 0)
            {
                return;
            }

            _content.CopyTo(sourceIndex, destination, destinationIndex, count);
        }

        private class LineCollection : RazorSourceLineCollection
        {
            private readonly DefaultRazorSourceDocument _document;
            private readonly int[] _lineStarts;

            public LineCollection(DefaultRazorSourceDocument document, int[] lineStarts)
            {
                _document = document;
                _lineStarts = lineStarts;
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

                    return new SourceLocation(_document.Filename, position, index, characterIndex: 0);
                }


                // Index is the complement of the line *after* the one we want, because BinarySearch tells
                // us where we'd put position *if* it were the start of a line.
                index = (~index) - 1;
                if (index == -1)
                {
                    // There's no preceding line, so it's based on the start of the string
                    return new SourceLocation(_document.Filename, position, 0, position);
                }
                else
                {
                    var characterIndex = position - _lineStarts[index];
                    return new SourceLocation(_document.Filename, position, index, characterIndex);
                }
            }

            public static int[] GetLineStarts(string text)
            {
                var starts = new List<int>();

                // We always consider a document to have at least a 0th line, even if it's empty.
                starts.Add(0);

                var unprocessedCR = false;

                // Length - 1 because we don't care if there was a linebreak as the last character.
                for (var i = 0; i < text.Length - 1; i++)
                {
                    var c = text[i];
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
}
