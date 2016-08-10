// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;

namespace Microsoft.AspNetCore.Rewrite.Internal.UrlMatches
{
    public class IntegerMatch : UrlMatch
    {
        public int Value { get; }
        public IntegerOperationType Operation { get; }
        public IntegerMatch(int value, IntegerOperationType operation)
        {
            Value = value;
            Operation = operation;
        }

        public IntegerMatch(string value, IntegerOperationType operation)
        {
            int compValue;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out compValue))
            {
                throw new FormatException("Syntax error for integers in comparison.");
            }
            Value = compValue;
            Operation = operation;
        }

        public override MatchResults Evaluate(string input, RewriteContext context)
        {
            int compValue;
            if (!int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out compValue))
            {
                return MatchResults.EmptyFailure;
            }

            switch (Operation)
            {
                case IntegerOperationType.Equal:
                    return compValue == Value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.Greater:
                    return compValue > Value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.GreaterEqual:
                    return compValue >= Value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.Less:
                    return compValue < Value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.LessEqual:
                    return compValue <= Value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.NotEqual:
                    return compValue != Value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                default:
                    return null;
            }
        }
    }
}
