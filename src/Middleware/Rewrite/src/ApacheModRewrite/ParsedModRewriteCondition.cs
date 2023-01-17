// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite;

internal sealed class ParsedModRewriteInput
{
    public bool Invert { get; set; }
    public ConditionType ConditionType { get; set; }
    public OperationType OperationType { get; set; }
    public string? Operand { get; set; }

    public ParsedModRewriteInput() { }

    public ParsedModRewriteInput(bool invert, ConditionType conditionType, OperationType operationType, string? operand)
    {
        Invert = invert;
        ConditionType = conditionType;
        OperationType = operationType;
        Operand = operand;
    }
}
