// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal class ZeroEntryJumpTable : JumpTable
    {
        private readonly int _defaultDestination;
        private readonly int _exitDestination;

        public ZeroEntryJumpTable(int defaultDestination, int exitDestination)
        {
            _defaultDestination = defaultDestination;
            _exitDestination = exitDestination;
        }

        public override int GetDestination(string path, PathSegment segment)
        {
            return segment.Length == 0 ? _exitDestination : _defaultDestination;
        }

        public override string DebuggerToString()
        {
            return $"{{ $+: {_defaultDestination}, $0: {_exitDestination} }}";
        }
    }
}
