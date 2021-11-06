// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions;

internal interface IPreallocatedAttributeTargetExtension : ICodeTargetExtension
{
    void WriteTagHelperHtmlAttribute(CodeRenderingContext context, PreallocatedTagHelperHtmlAttributeIntermediateNode node);

    void WriteTagHelperHtmlAttributeValue(CodeRenderingContext context, PreallocatedTagHelperHtmlAttributeValueIntermediateNode node);

    void WriteTagHelperProperty(CodeRenderingContext context, PreallocatedTagHelperPropertyIntermediateNode node);

    void WriteTagHelperPropertyValue(CodeRenderingContext context, PreallocatedTagHelperPropertyValueIntermediateNode node);
}
