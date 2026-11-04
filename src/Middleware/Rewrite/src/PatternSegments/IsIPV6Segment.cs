// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class IsIPV6Segment : PatternSegment
{
    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        if (context.HttpContext.Connection.RemoteIpAddress == null)
        {
            return "off";
        }
        return context.HttpContext.Connection.RemoteIpAddress.AddressFamily == AddressFamily.InterNetworkV6 ? "on" : "off";
    }
}
