// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite.Operands
{
    public class RegexOperand : Operand
    {
        public Regex RegexOperation { get; }

        public RegexOperand(Regex regex)
        {
            RegexOperation = regex;
        }

        public override bool? CheckOperation(Match previous, string concatTestString, IFileProvider fileProvider)
        {
            previous = RegexOperation.Match(concatTestString);
            return previous.Success;
        }
    }
}
