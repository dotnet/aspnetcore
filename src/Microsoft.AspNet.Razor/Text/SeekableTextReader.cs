// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Razor.Text
{
    public class SeekableTextReader : TextReader, ITextDocument
    {
        private int _position = 0;
        private LineTrackingStringBuffer _buffer = new LineTrackingStringBuffer();
        private SourceLocation _location = SourceLocation.Zero;
        private char? _current;

        public SeekableTextReader(string content)
        {
            _buffer.Append(content);
            UpdateState();
        }

        public SeekableTextReader([NotNull] TextReader source)
            : this(source.ReadToEnd())
        {
        }

        public SeekableTextReader([NotNull] ITextBuffer buffer)
            : this(buffer.ReadToEnd())
        {
        }

        public SourceLocation Location
        {
            get { return _location; }
        }

        public int Length
        {
            get { return _buffer.Length; }
        }

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

        internal LineTrackingStringBuffer Buffer
        {
            get { return _buffer; }
        }

        public override int Read()
        {
            if (_current == null)
            {
                return -1;
            }
            var chr = _current.Value;
            _position++;
            UpdateState();
            return chr;
        }

        public override int Peek()
        {
            if (_current == null)
            {
                return -1;
            }
            return _current.Value;
        }

        private void UpdateState()
        {
            if (_position < _buffer.Length)
            {
                LineTrackingStringBuffer.CharacterReference chr = _buffer.CharAt(_position);
                _current = chr.Character;
                _location = chr.Location;
            }
            else if (_buffer.Length == 0)
            {
                _current = null;
                _location = SourceLocation.Zero;
            }
            else
            {
                _current = null;
                _location = _buffer.EndLocation;
            }
        }
    }
}
