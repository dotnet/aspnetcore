// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlRewrite.UrlMatches
{
    public class RegexMatch : UrlMatch
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(1);

        public Regex Match { get; }

        public RegexMatch(Regex match, bool negate)
        {
            Match = match;
            Negate = negate;
        }

        public override MatchResults Evaluate(string pattern, RewriteContext context)
        {
            var res = Match.Match(pattern);
            return new MatchResults { BackReference = res.Groups, Success = (res.Success != Negate)};
        }
    }
}
