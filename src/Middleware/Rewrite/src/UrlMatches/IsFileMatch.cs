// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.UrlMatches
{
    internal class IsFileMatch : UrlMatch
    {
        public IsFileMatch(bool negate)
        {
            Negate = negate;
        }

        public override MatchResults Evaluate(string pattern, RewriteContext context)
        {
            var res = context.StaticFileProvider.GetFileInfo(pattern).Exists;
            return new MatchResults { Success = (res != Negate) };
        }
    }
}
