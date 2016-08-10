// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public class Condition
    {
        public Pattern Input { get; set; }
        public UrlMatch Match { get; set; }
        public bool OrNext { get; set; }

        public MatchResults Evaluate(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            var pattern = Input.Evaluate(context, ruleMatch, condMatch);
            return Match.Evaluate(pattern, context);
        }
    }
}
