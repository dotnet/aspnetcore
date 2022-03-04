// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.UrlMatches
{
    internal class FileSizeMatch : UrlMatch
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
}
