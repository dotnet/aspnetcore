// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal interface IDesignTimeDirectiveTargetExtension : ICodeTargetExtension
    {
        void WriteDesignTimeDirective(CodeRenderingContext context, DesignTimeDirectiveIntermediateNode node);
    }
}
