// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.AspNetCore.Rewrite;

namespace Microsoft.AspNetCore.Rewrite.UrlMatches
{
    internal class IntegerMatch : UrlMatch
    {
        private readonly int _value;
        private readonly IntegerOperationType _operation;
        public IntegerMatch(int value, IntegerOperationType operation)
        {
            _value = value;
            _operation = operation;
        }

        public IntegerMatch(string value, IntegerOperationType operation)
        {
            int compValue;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out compValue))
            {
                throw new FormatException(Resources.Error_IntegerMatch_FormatExceptionMessage);
            }
            _value = compValue;
            _operation = operation;
        }

        public override MatchResults Evaluate(string input, RewriteContext context)
        {
            int compValue;
            if (!int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out compValue))
            {
                return MatchResults.EmptyFailure;
            }

            switch (_operation)
            {
                case IntegerOperationType.Equal:
                    return compValue == _value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.Greater:
                    return compValue > _value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.GreaterEqual:
                    return compValue >= _value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.Less:
                    return compValue < _value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.LessEqual:
                    return compValue <= _value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                case IntegerOperationType.NotEqual:
                    return compValue != _value ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
                default:
                    return null;
            }
        }
    }
}
