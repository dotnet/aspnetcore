// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.UrlMatches;

internal sealed class IsFileMatch : UrlMatch
{
    public IsFileMatch(bool negate)
    {
        Negate = negate;
    }

    public override MatchResults Evaluate(string pattern, RewriteContext context)
    {
        var res = context.StaticFileProvider.GetFileInfo(pattern).Exists;
        return new MatchResults(success: res != Negate);
    }
}
