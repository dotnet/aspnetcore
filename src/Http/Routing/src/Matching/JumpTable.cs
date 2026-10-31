// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Routing.Matching;

[DebuggerDisplay("{DebuggerToString(),nq}")]
internal abstract class JumpTable
{
    public abstract int GetDestination(string path, PathSegment segment);

    public virtual string DebuggerToString()
    {
        return GetType().Name;
    }
}
