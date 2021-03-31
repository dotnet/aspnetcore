// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.AspNetCore.Rewrite.UrlMatches
{
    internal class StringMatch : UrlMatch
    {
        private readonly string _value;
        private readonly StringOperationType _operation;
        private readonly StringComparison _stringComparison;

        public StringMatch(string value, StringOperationType operation, bool ignoreCase)
        {
            _value = value;
            _operation = operation;
            _stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        }

        public override MatchResults Evaluate(string input, RewriteContext context)
        {
            switch (_operation)
            {
                case StringOperationType.Equal:
                    return string.Compare(input, _value, _stringComparison) == 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.Greater:
                    return string.Compare(input, _value, _stringComparison) > 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.GreaterEqual:
                    return string.Compare(input, _value, _stringComparison) >= 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.Less:
                    return string.Compare(input, _value, _stringComparison) < 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case StringOperationType.LessEqual:
                    return string.Compare(input, _value, _stringComparison) <= 0 ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                default:
                    throw new ArgumentOutOfRangeException("operation"); // Will never be thrown
            }
        }
    }
}
