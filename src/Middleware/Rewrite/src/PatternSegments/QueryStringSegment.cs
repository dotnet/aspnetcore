// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.PatternSegments
{
    internal class QueryStringSegment : PatternSegment
    {
        public override string Evaluate(RewriteContext context, BackReferenceCollection ruleBackRefernces, BackReferenceCollection conditionBackReferences)
        {
            var queryString = context.HttpContext.Request.QueryString.ToString();

            if (!string.IsNullOrEmpty(queryString))
            {
                return queryString.Substring(1);
            }

            return queryString;
        }
    }
}
