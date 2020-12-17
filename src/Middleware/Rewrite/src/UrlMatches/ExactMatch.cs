// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

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
}
