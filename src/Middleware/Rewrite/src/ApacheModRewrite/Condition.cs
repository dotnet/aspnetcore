// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal class Condition
    {
        public Condition(Pattern input, UrlMatch match, bool orNext)
        {
            Input = input;
            Match = match;
            OrNext = orNext;
        }

        public Pattern Input { get; }
        public UrlMatch Match { get; }
        public bool OrNext { get; }

        public MatchResults Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
        {
            var pattern = Input.Evaluate(context, ruleBackReferences, conditionBackReferences);
            return Match.Evaluate(pattern, context);
        }
    }
}
