// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Globalization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.PatternSegments
{
    public class RemotePortSegment : PatternSegment
    {
        public override string Evaluate(HttpContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            return context.Connection.RemotePort.ToString(CultureInfo.InvariantCulture);
        }
    }
}
