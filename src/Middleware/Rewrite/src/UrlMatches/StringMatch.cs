// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.AspNetCore.Rewrite.UrlMatches;

internal sealed class StringMatch : UrlMatch
{
    private readonly string _value;
    private readonly StringOperationType _operation;
    private readonly StringComparison _stringComparison;

    public StringMatch(string value, StringOperationType operation, bool ignoreCase, bool negate)
    {
        _value = value;
        if (operation < StringOperationType.Equal || operation > StringOperationType.LessEqual)
        {
            throw new ArgumentOutOfRangeException(nameof(operation));
        }

        _operation = operation;
        _stringComparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        Negate = negate;
    }

    public override MatchResults Evaluate(string input, RewriteContext context)
    {
        bool success;
        switch (_operation)
        {
            case StringOperationType.Equal:
                success = string.Compare(input, _value, _stringComparison) == 0;
                break;
            case StringOperationType.Greater:
                success = string.Compare(input, _value, _stringComparison) > 0;
                break;
            case StringOperationType.GreaterEqual:
                success = string.Compare(input, _value, _stringComparison) >= 0;
                break;
            case StringOperationType.Less:
                success = string.Compare(input, _value, _stringComparison) < 0;
                break;
            case StringOperationType.LessEqual:
                success = string.Compare(input, _value, _stringComparison) <= 0;
                break;
            default:
                Debug.Fail("This is never reached.");
                throw new InvalidOperationException(); // Will never be thrown
        }

        return success != Negate ? MatchResults.EmptySuccess : MatchResults.EmptyFailure;
    }
}
