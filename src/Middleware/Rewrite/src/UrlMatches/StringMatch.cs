// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.UrlMatches
{
    internal class StringMatch : UrlMatch
    {
        private readonly string _value;
        private readonly StringOperationType _operation;
        private readonly bool _ignoreCase;

        public StringMatch(string value, StringOperationType operation, bool ignoreCase)
        {
            _value = value;
            _operation = operation;
            _ignoreCase = ignoreCase;
        }

        public override MatchResults Evaluate(string input, RewriteContext context)
        {
            switch (_operation)
            {
                case StringOperationType.Equal:
                    return string.Compare(input, _value, _ignoreCase) == 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.Greater:
                    return string.Compare(input, _value, _ignoreCase) > 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.GreaterEqual:
                    return string.Compare(input, _value, _ignoreCase) >= 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.Less:
                    return string.Compare(input, _value, _ignoreCase) < 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.LessEqual:
                    return string.Compare(input, _value, _ignoreCase) <= 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                default:
                    return null;
            }
        }
    }
}
