// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNet.Razor.Text
{
    public struct LookaheadToken : IDisposable
    {
        private readonly ITextBuffer _buffer;
        private readonly int _position;

        private bool _accepted;

        public LookaheadToken(ITextBuffer buffer)
        {
            _buffer = buffer;
            _position = buffer.Position;

            _accepted = false;
        }

        public void Accept()
        {
            _accepted = true;
        }

        public void Dispose()
        {
            if (!_accepted)
            {
                _buffer.Position = _position;
            }
        }
    }
}
