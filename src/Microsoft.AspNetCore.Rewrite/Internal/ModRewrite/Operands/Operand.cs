// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Text.RegularExpressions;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Rewrite.Internal.ModRewrite.Operands
{
    public abstract class Operand
    {
        public abstract bool? CheckOperation(Match previous, string concatTestString, IFileProvider fileProvider);
    }
}
