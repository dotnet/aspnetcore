// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.AspNetCore.Razor.Language.Extensions;

namespace Microsoft.AspNetCore.Razor.Language.Extensions
{
    internal interface IPreallocatedAttributeTargetExtension : ICodeTargetExtension
    {
        void WriteDeclarePreallocatedTagHelperHtmlAttribute(CSharpRenderingContext context, DeclarePreallocatedTagHelperHtmlAttributeIntermediateNode node);

        void WriteAddPreallocatedTagHelperHtmlAttribute(CSharpRenderingContext context, AddPreallocatedTagHelperHtmlAttributeIntermediateNode node);

        void WriteDeclarePreallocatedTagHelperAttribute(CSharpRenderingContext context, DeclarePreallocatedTagHelperAttributeIntermediateNode node);

        void WriteSetPreallocatedTagHelperProperty(CSharpRenderingContext context, SetPreallocatedTagHelperPropertyIntermediateNode node);
    }
}
