// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.UrlMatches;

internal sealed class ExactMatch : UrlMatch
{
    private readonly bool _ignoreCase;
    private readonly string _stringMatch;

    public ExactMatch(bool ignoreCase, string input, bool negate)
    {
        _ignoreCase = ignoreCase;
        _stringMatch = input;
        Negate = negate;
    }

    public override MatchResults Evaluate(string pattern, RewriteContext context)
    {
        var pathMatch = string.Equals(pattern, _stringMatch, _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        var success = pathMatch != Negate;
        if (success)
        {
            return new MatchResults(success, new BackReferenceCollection(pattern));
        }
        else
        {
            return MatchResults.EmptyFailure;
        }
    }
}
