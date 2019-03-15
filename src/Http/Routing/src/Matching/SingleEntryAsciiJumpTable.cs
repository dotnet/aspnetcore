// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.AspNetCore.Routing.Matching
{
    // Optimized implementation for cases where we know that we're
    // comparing to ASCII.
    internal class SingleEntryAsciiJumpTable : JumpTable
    {
        private readonly int _defaultDestination;
        private readonly int _exitDestination;
        private readonly string _text;
        private readonly int _destination;

        public SingleEntryAsciiJumpTable(
            int defaultDestination,
            int exitDestination,
            string text,
            int destination)
        {
            _defaultDestination = defaultDestination;
            _exitDestination = exitDestination;
            _text = text;
            _destination = destination;
        }

        public unsafe override int GetDestination(string path, PathSegment segment)
        {
            var length = segment.Length;
            if (length == 0)
            {
                return _exitDestination;
            }

            var text = _text;
            if (length != text.Length)
            {
                return _defaultDestination;
            }

            var a = path.AsSpan(segment.Start, length);
            var b = text.AsSpan();

            return Ascii.AsciiIgnoreCaseEquals(a, b, length) ? _destination : _defaultDestination;
        }

        public override string DebuggerToString()
        {
            return $"{{ {_text}: {_destination}, $+: {_defaultDestination}, $0: {_exitDestination} }}";
        }
    }
}
