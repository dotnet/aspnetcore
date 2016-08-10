// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlMatches
{
    public class StringMatch : UrlMatch
    {
        public string Value { get; set; }
        public StringOperationType Operation { get; set; }
        public bool IgnoreCase { get; set; }
        public StringMatch(string value, StringOperationType operation)
        {
            Value = value;
            Operation = operation;
        }

        public override MatchResults Evaluate(string input, RewriteContext context)
        {
            switch (Operation)
            {
                case StringOperationType.Equal:
                    return string.Compare(input, Value, IgnoreCase) == 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.Greater:
                    return string.Compare(input, Value, IgnoreCase) > 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.GreaterEqual:
                    return string.Compare(input, Value, IgnoreCase) >= 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.Less:
                    return string.Compare(input, Value, IgnoreCase) < 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.LessEqual:
                    return string.Compare(input, Value, IgnoreCase) <= 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                default:
                    return null;
            }
        }
    }
}
