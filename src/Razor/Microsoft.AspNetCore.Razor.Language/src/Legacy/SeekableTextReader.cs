// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language.Legacy
{
    internal class SeekableTextReader : TextReader, ITextDocument
    {
        private readonly LineTrackingStringBuffer _buffer;
        private int _position = 0;
        private int _current;
        private SourceLocation _location;

        public SeekableTextReader(string source, string filePath) : this(source.ToCharArray(), filePath) { }

        public SeekableTextReader(char[] source, string filePath)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            _buffer = new LineTrackingStringBuffer(source, filePath);
            UpdateState();
        }

        public SourceLocation Location => _location;

        public int Length => _buffer.Length;

        public int Position
        {
            get { return _position; }
            set
            {
                if (_position != value)
                {
                    _position = value;
                    UpdateState();
                }
            }
        }

        public override int Read()
        {
            var c = _current;
            _position++;
            UpdateState();
            return c;
        }

        public override int Peek() => _current;

        private void UpdateState()
        {
            if (_position < _buffer.Length)
            {
                var chr = _buffer.CharAt(_position);
                _current = chr.Character;
                _location = chr.Location;
            }
            else if (_buffer.Length == 0)
            {
                _current = -1;
                _location = SourceLocation.Zero;
            }
            else
            {
                _current = -1;
                _location = _buffer.EndLocation;
            }
        }
    }
}
