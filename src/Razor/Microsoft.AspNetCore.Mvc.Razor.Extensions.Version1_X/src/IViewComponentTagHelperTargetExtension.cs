// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version1_X;

public interface IViewComponentTagHelperTargetExtension : ICodeTargetExtension
{
    void WriteViewComponentTagHelper(CodeRenderingContext context, ViewComponentTagHelperIntermediateNode node);
}
