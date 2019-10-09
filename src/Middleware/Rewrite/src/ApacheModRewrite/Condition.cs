// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal class Condition
    {
        public Pattern Input { get; set; }
        public UrlMatch Match { get; set; }
        public bool OrNext { get; set; }

        public MatchResults Evaluate(RewriteContext context, BackReferenceCollection ruleBackReferences, BackReferenceCollection conditionBackReferences)
        {
            var pattern = Input.Evaluate(context, ruleBackReferences, conditionBackReferences);
            return Match.Evaluate(pattern, context);
        }
    }
}