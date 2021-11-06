// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Mvc.Razor.Extensions.Version2_X;

public interface IInjectTargetExtension : ICodeTargetExtension
{
    void WriteInjectProperty(CodeRenderingContext context, InjectIntermediateNode node);
}
