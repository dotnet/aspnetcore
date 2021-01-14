// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    public interface IDefaultTagHelperTargetExtension : ICodeTargetExtension
    {
        void WriteTagHelperBody(CodeRenderingContext context, DefaultTagHelperBodyIntermediateNode node);

        void WriteTagHelperCreate(CodeRenderingContext context, DefaultTagHelperCreateIntermediateNode node);

        void WriteTagHelperExecute(CodeRenderingContext context, DefaultTagHelperExecuteIntermediateNode node);

        void WriteTagHelperHtmlAttribute(CodeRenderingContext context, DefaultTagHelperHtmlAttributeIntermediateNode node);

        void WriteTagHelperProperty(CodeRenderingContext context, DefaultTagHelperPropertyIntermediateNode node);

        void WriteTagHelperRuntime(CodeRenderingContext context, DefaultTagHelperRuntimeIntermediateNode node);
    }
}
