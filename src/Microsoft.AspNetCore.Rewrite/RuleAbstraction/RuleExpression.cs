// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Rewrite.Operands;

namespace Microsoft.AspNetCore.Rewrite.RuleAbstraction
{
    public class RuleExpression
    {
        public RegexOperand Operand { get; set; }
        public bool Invert { get; set; }
    }
}
