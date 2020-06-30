// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal interface IMetadataAttributeTargetExtension : ICodeTargetExtension
    {
        void WriteRazorCompiledItemAttribute(CodeRenderingContext context, RazorCompiledItemAttributeIntermediateNode node);

        void WriteRazorSourceChecksumAttribute(CodeRenderingContext context, RazorSourceChecksumAttributeIntermediateNode node);

        void WriteRazorCompiledItemMetadataAttribute(CodeRenderingContext context, RazorCompiledItemMetadataAttributeIntermediateNode node);
    }
}
