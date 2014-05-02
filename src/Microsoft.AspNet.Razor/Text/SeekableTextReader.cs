// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED *AS IS* BASIS, WITHOUT WARRANTIES OR
// CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING
// WITHOUT LIMITATION ANY IMPLIED WARRANTIES OR CONDITIONS OF
// TITLE, FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABLITY OR
// NON-INFRINGEMENT.
// See the Apache 2 License for the specific language governing
// permissions and limitations under the License.

using System.IO;

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

        public SeekableTextReader(TextReader source)
            : this(source.ReadToEnd())
        {
        }

        public SeekableTextReader(ITextBuffer buffer)
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
            char chr = _current.Value;
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
