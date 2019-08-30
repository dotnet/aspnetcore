// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal class HttpHostNameSegment : PatternSegment
    {
        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            var host = context.HttpContext.Request.Headers[HeaderNames.Host].ToString();
            return host.Remove(host.IndexOf(':'));
        }
    }
}
