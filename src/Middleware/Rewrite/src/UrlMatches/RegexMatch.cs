// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;

namespace Microsoft.AspNetCore.Rewrite.UrlMatches
{
    internal class RegexMatch : UrlMatch
    {
        private readonly Regex _match;

        public RegexMatch(Regex match, bool negate)
        {
            _match = match;
            Negate = negate;
        }

        public override MatchResults Evaluate(string pattern, RewriteContext context)
        {
            var res = _match.Match(pattern);
            return new MatchResults { BackReferences = new BackReferenceCollection(res.Groups), Success = (res.Success != Negate) };
        }
    }
}
