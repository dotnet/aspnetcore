// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Rewrite.Internal.ModRewrite.Operands;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite
{
    /// <summary>
    ///  Converts a parsed expression into a mod_rewrite condition. 
    /// </summary>
    public class ExpressionCreator
    {
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(1);
        public static ConditionExpression CreateConditionExpression(ParsedModRewriteExpression pce, ConditionFlags flags)
        {
            var condExp = new ConditionExpression();
            condExp.Invert = pce.Invert;
            if (pce.Type == ConditionType.Regex)
            {
                // TODO make nullable?
                if (flags != null && flags.HasFlag(ConditionFlagType.NoCase))
                {
                    condExp.Operand = new RegexOperand(new Regex(pce.Operand, RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout));
                }
                else
                {
                    condExp.Operand = new RegexOperand(new Regex(pce.Operand, RegexOptions.Compiled, RegexTimeout));
                }
            }
            else if (pce.Type == ConditionType.IntComp)
            {
                switch (pce.Operation)
                {
                    case OperationType.Equal:
                        condExp.Operand = new IntegerOperand(pce.Operand, IntegerOperationType.Equal);
                        break;
                    case OperationType.Greater:
                        condExp.Operand = new IntegerOperand(pce.Operand, IntegerOperationType.Greater);
                        break;
                    case OperationType.GreaterEqual:
                        condExp.Operand = new IntegerOperand(pce.Operand, IntegerOperationType.GreaterEqual);
                        break;
                    case OperationType.Less:
                        condExp.Operand = new IntegerOperand(pce.Operand, IntegerOperationType.Less);
                        break;
                    case OperationType.LessEqual:
                        condExp.Operand = new IntegerOperand(pce.Operand, IntegerOperationType.LessEqual);
                        break;
                    case OperationType.NotEqual:
                        condExp.Operand = new IntegerOperand(pce.Operand, IntegerOperationType.NotEqual);
                        break;
                    default:
                        throw new ArgumentException("No defined operation for integer comparison.");
                }
            }
            else if (pce.Type == ConditionType.StringComp)
            {
                switch (pce.Operation)
                {
                    case OperationType.Equal:
                        condExp.Operand = new StringOperand(pce.Operand, StringOperationType.Equal);
                        break;
                    case OperationType.Greater:
                        condExp.Operand = new StringOperand(pce.Operand, StringOperationType.Greater);
                        break;
                    case OperationType.GreaterEqual:
                        condExp.Operand = new StringOperand(pce.Operand, StringOperationType.GreaterEqual);
                        break;
                    case OperationType.Less:
                        condExp.Operand = new StringOperand(pce.Operand, StringOperationType.Less);
                        break;
                    case OperationType.LessEqual:
                        condExp.Operand = new StringOperand(pce.Operand, StringOperationType.LessEqual);
                        break;
                    default:
                        throw new ArgumentException("No defined operation for string comparison.");
                }
            }
            else
            {
                switch (pce.Operation)
                {
                    case OperationType.Directory:
                        condExp.Operand = new PropertyOperand(PropertyOperationType.Directory);
                        break;
                    case OperationType.RegularFile:
                        condExp.Operand = new PropertyOperand(PropertyOperationType.RegularFile);
                        break;
                    case OperationType.ExistingFile:
                        condExp.Operand = new PropertyOperand(PropertyOperationType.ExistingFile);
                        break;
                    case OperationType.SymbolicLink:
                        condExp.Operand = new PropertyOperand(PropertyOperationType.SymbolicLink);
                        break;
                    case OperationType.Size:
                        condExp.Operand = new PropertyOperand(PropertyOperationType.Size);
                        break;
                    case OperationType.ExistingUrl:
                        condExp.Operand = new PropertyOperand(PropertyOperationType.ExistingUrl);
                        break;
                    case OperationType.Executable:
                        condExp.Operand = new PropertyOperand(PropertyOperationType.Executable);
                        break;
                    default:
                        throw new ArgumentException("No defined operation for property comparison.");
                }
            }
            return condExp;
        }
        public static RuleExpression CreateRuleExpression(ParsedModRewriteExpression pce, RuleFlags flags)
        {
            var ruleExp = new RuleExpression();
            ruleExp.Invert = pce.Invert;
            if (flags.HasFlag(RuleFlagType.NoCase))
            {
                ruleExp.Operand = new RegexOperand(new Regex(pce.Operand, RegexOptions.IgnoreCase | RegexOptions.Compiled, RegexTimeout));
            }
            else
            {
                ruleExp.Operand = new RegexOperand(new Regex(pce.Operand, RegexOptions.Compiled, RegexTimeout));
            }
            return ruleExp;
        }
    }
}
