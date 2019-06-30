// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Sockets;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal class IsIPV6Segment : PatternSegment
    {
        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            if (context.HttpContext.Connection.RemoteIpAddress == null)
            {
                return "off";
            }
            return context.HttpContext.Connection.RemoteIpAddress.AddressFamily == AddressFamily.InterNetworkV6 ? "on" : "off";
        }
    }
}
