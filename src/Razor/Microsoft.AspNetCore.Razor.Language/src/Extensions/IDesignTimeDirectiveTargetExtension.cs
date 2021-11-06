// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal interface IDesignTimeDirectiveTargetExtension : ICodeTargetExtension
{
    void WriteDesignTimeDirective(CodeRenderingContext context, DesignTimeDirectiveIntermediateNode node);
}
