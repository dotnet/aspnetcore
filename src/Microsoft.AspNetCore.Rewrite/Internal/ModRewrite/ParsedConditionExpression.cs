// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    public class ParsedModRewriteExpression
    {
        public bool Invert { get; set; }
        public ConditionType Type { get; set; }
        public OperationType Operation { get; set; }
        public string Operand { get; set; }
        public ParsedModRewriteExpression(bool invert, ConditionType type, OperationType operation, string operand)
        {
            Invert = invert;
            Type = type;
            Operation = operation;
            Operand = operand;
        }

        public ParsedModRewriteExpression() { }
    }
}
