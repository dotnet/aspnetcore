// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal
{
    public abstract class UrlAction
    {
        public Pattern Url { get; set; }
        public abstract RuleResult ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch);
    }
}
