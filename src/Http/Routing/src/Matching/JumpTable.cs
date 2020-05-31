// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Matching
{
    internal delegate int JumpTableDelegate(ReadOnlySpan<char> path);

    [DebuggerDisplay("{DebuggerToString(),nq}")]
    internal abstract class JumpTable
    {
        public abstract int GetDestination(ReadOnlySpan<char> path);

        public virtual string DebuggerToString()
        {
            return GetType().Name;
        }
    }
}
