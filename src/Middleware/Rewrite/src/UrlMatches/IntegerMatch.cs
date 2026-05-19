// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;

namespace Microsoft.AspNetCore.Rewrite.UrlMatches;

internal sealed class IntegerMatch : UrlMatch
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

        if (operation < IntegerOperationType.Equal || operation > IntegerOperationType.NotEqual)
        {
            throw new ArgumentOutOfRangeException(nameof(operation));
        }

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
                Debug.Fail("This is never reached.");
                throw new InvalidOperationException(); // Will never be thrown
        }
    }
}
