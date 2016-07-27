// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite.Operands
{
    public class StringOperand : Operand
    {
        public string Value { get; set; }
        public StringOperationType Operation { get; set; } 

        public StringOperand(string value, StringOperationType operation)
        {
            Value = value;
            Operation = operation;
        }

        public override bool? CheckOperation(Match previous, string concatTestString, IFileProvider fileProvider)
        {
            switch (Operation)
            {
                case StringOperationType.Equal:
                    return concatTestString.CompareTo(Value) == 0;
                case StringOperationType.Greater:
                    return concatTestString.CompareTo(Value) > 0;
                case StringOperationType.GreaterEqual:
                    return concatTestString.CompareTo(Value) >= 0;
                case StringOperationType.Less:
                    return concatTestString.CompareTo(Value) < 0;
                case StringOperationType.LessEqual:
                    return concatTestString.CompareTo(Value) <= 0;
                default:
                    return null;
            }
        }
    }
}
