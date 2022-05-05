// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.UrlMatches;

internal sealed class FileSizeMatch : UrlMatch
{
    public FileSizeMatch(bool negate)
    {
        Negate = negate;
    }

    public override MatchResults Evaluate(string input, RewriteContext context)
    {
        var fileInfo = context.StaticFileProvider.GetFileInfo(input);
        return fileInfo.Exists && fileInfo.Length > 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
    }
}
