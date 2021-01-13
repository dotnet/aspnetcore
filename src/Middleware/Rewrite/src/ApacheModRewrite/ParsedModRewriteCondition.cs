// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Microsoft.AspNetCore.Rewrite.ApacheModRewrite
{
    internal class ParsedModRewriteInput
    {
        public bool Invert { get; set; }
        public ConditionType ConditionType { get; set; }
        public OperationType OperationType { get; set; }
        public string Operand { get; set; }

        public ParsedModRewriteInput() { }

        public ParsedModRewriteInput(bool invert, ConditionType conditionType, OperationType operationType, string operand)
        {
            Invert = invert;
            ConditionType = conditionType;
            OperationType = operationType;
            Operand = operand;
        }
    }
}
