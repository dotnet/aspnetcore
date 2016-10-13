// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Razor.Evolution.Legacy
{
    internal class StringTextBuffer : ITextBuffer, IDisposable
    {
        private string _buffer;
        public bool Disposed { get; set; }

        public StringTextBuffer(string buffer)
        {
            _buffer = buffer;
        }

        public int Length
        {
            get { return _buffer.Length; }
        }

        public int Position { get; set; }

        public int Read()
        {
            if (Position >= _buffer.Length)
            {
                return -1;
            }
            return _buffer[Position++];
        }

        public int Peek()
        {
            if (Position >= _buffer.Length)
            {
                return -1;
            }
            return _buffer[Position];
        }

        public void Dispose()
        {
            Disposed = true;
        }

        public object VersionToken
        {
            get { return _buffer; }
        }
    }
}
