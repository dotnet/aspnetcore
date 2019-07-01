// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.UrlMatches
{
    internal class ExactMatch : UrlMatch
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
            var pathMatch = string.Compare(pattern, _stringMatch, _ignoreCase);
            var success = ((pathMatch == 0) != Negate);
            if (success)
            {
                return new MatchResults { Success = success, BackReferences = new BackReferenceCollection(pattern) };
            }
            else
            {
                return MatchResults.EmptyFailure;
            }
        }
    }
}
