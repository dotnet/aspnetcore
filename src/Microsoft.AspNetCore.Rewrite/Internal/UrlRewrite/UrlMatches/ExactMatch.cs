// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.UrlMatches
{
    public class ExactMatch : UrlMatch
    {
        public bool IgnoreCase { get; }
        public string StringMatch { get; }

        public ExactMatch(bool ignoreCase, string input, bool negate)
        {
            IgnoreCase = ignoreCase;
            StringMatch = input;
            Negate = negate;
        }

        public override MatchResults Evaluate(string pattern, RewriteContext context)
        {
            var pathMatch = string.Compare(pattern, StringMatch, IgnoreCase);
            return new MatchResults { Success = ((pathMatch == 0) != Negate) };
        }
    }
}
