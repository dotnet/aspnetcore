// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite.Operands
{
    public class IntegerOperand : Operand
    {
        public int Value { get; }
        public IntegerOperationType Operation { get; }
        public IntegerOperand(int value, IntegerOperationType operation)
        {
            Value = value;
            Operation = operation;
        }

        public IntegerOperand(string value, IntegerOperationType operation)
        {
            int compValue;
            if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out compValue))
            {
                throw new FormatException("Syntax error for integers in comparison.");
            }
            Value = compValue;
            Operation = operation;
        }

        public override bool? CheckOperation(Match previous, string testString, IFileProvider fileProvider)
        {
            int compValue;
            if (!int.TryParse(testString, NumberStyles.None, CultureInfo.InvariantCulture, out compValue))
            {
                return false;
            }
            switch (Operation)
            {
                case IntegerOperationType.Equal:
                    return compValue == Value;
                case IntegerOperationType.Greater:
                    return compValue > Value;
                case IntegerOperationType.GreaterEqual:
                    return compValue >= Value;
                case IntegerOperationType.Less:
                    return compValue < Value;
                case IntegerOperationType.LessEqual:
                    return compValue <= Value;
                case IntegerOperationType.NotEqual:
                    return compValue != Value;
                default:
                    return null;
            }
        }
    }
}
