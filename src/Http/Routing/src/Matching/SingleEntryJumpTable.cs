// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class SingleEntryJumpTable : JumpTable
    {
        private readonly int _defaultDestination;
        private readonly int _exitDestination;
        private readonly string _text;
        private readonly int _destination;

        public SingleEntryJumpTable(
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

        public override int GetDestination(ReadOnlySpan<char> path)
        {
            if (path.Length == 0)
            {
                return _exitDestination;
            }

            if (path.Equals(_text, StringComparison.OrdinalIgnoreCase))
            {
                return _destination;
            }

            return _defaultDestination;
        }

        public override string DebuggerToString()
        {
            return $"{{ {_text}: {_destination}, $+: {_defaultDestination}, $0: {_exitDestination} }}";
        }
    }
}
