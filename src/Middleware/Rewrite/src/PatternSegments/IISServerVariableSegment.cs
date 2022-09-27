// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Rewrite.PatternSegments;

internal sealed class IISServerVariableSegment : PatternSegment
{
    private readonly string _variableName;
    private readonly Func<PatternSegment> _fallbackThunk;

    public IISServerVariableSegment(string variableName, Func<PatternSegment> fallbackThunk)
    {
        _variableName = variableName;
        _fallbackThunk = fallbackThunk;
    }

    public override string? Evaluate(RewriteContext context, BackReferenceCollection? ruleBackReferences, BackReferenceCollection? conditionBackReferences)
    {
        return context.HttpContext.GetServerVariable(_variableName) ?? _fallbackThunk().Evaluate(context, ruleBackReferences, conditionBackReferences);
    }
}
