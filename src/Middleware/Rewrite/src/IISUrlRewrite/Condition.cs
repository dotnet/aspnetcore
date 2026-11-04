// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.IISUrlRewrite;

internal class Condition
{
    public Condition(Pattern input, UrlMatch match)
    {
        Input = input;
        Match = match;
    }

    public Pattern Input { get; }
    public UrlMatch Match { get; }

    public MatchResults Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        var pattern = Input.Evaluate(context, ruleBackReferences, conditionBackReferences);
        return Match.Evaluate(pattern, context);
    }
}
