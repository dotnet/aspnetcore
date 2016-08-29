// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlActions
{
    public class VoidAction : UrlAction
    {
        public RuleTermination Result { get; }

        public VoidAction(RuleTermination result)
        {
            Result = result;
        }
        // Explicitly say that nothing happens
        public override void ApplyAction(RewriteContext context, MatchResults ruleMatch, MatchResults condMatch)
        {
            context.Result = Result;
        }
    }
}
