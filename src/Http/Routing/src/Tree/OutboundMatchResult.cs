// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Routing.Tree;

internal readonly struct OutboundMatchResult
{
    public OutboundMatchResult(OutboundMatch match, bool isFallbackMatch)
    {
        Match = match;
        IsFallbackMatch = isFallbackMatch;
    }

    public OutboundMatch Match { get; }

    public bool IsFallbackMatch { get; }
}
