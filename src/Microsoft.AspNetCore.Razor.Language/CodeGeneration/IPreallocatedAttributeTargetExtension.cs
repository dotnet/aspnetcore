// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal interface IPreallocatedAttributeTargetExtension : IRuntimeTargetExtension
    {
        void WriteDeclarePreallocatedTagHelperHtmlAttribute(CSharpRenderingContext context, DeclarePreallocatedTagHelperHtmlAttributeIRNode node);

        void WriteAddPreallocatedTagHelperHtmlAttribute(CSharpRenderingContext context, AddPreallocatedTagHelperHtmlAttributeIRNode node);

        void WriteDeclarePreallocatedTagHelperAttribute(CSharpRenderingContext context, DeclarePreallocatedTagHelperAttributeIRNode node);

        void WriteSetPreallocatedTagHelperProperty(CSharpRenderingContext context, SetPreallocatedTagHelperPropertyIRNode node);
    }
}
