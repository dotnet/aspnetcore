// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

internal sealed class Condition
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
