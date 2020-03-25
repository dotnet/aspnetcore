// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Matching
{
    [DebuggerDisplay("{DebuggerToString(),nq}")]
    internal abstract class JumpTable
    {
        public abstract int GetDestination(string path, PathSegment segment);

        public virtual string DebuggerToString()
        {
            return GetType().Name;
        }
    }
}
