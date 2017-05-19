// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class TagHelperWriter
    {
        public abstract void WriteDeclareTagHelperFields(CSharpRenderingContext context, DeclareTagHelperFieldsIRNode node);

        public abstract void WriteTagHelper(CSharpRenderingContext context, TagHelperIRNode node);

        public abstract void WriteTagHelperBody(CSharpRenderingContext context, TagHelperBodyIRNode node);

        public abstract void WriteCreateTagHelper(CSharpRenderingContext context, CreateTagHelperIRNode node);

        public abstract void WriteAddTagHelperHtmlAttribute(CSharpRenderingContext context, AddTagHelperHtmlAttributeIRNode node);

        public abstract void WriteSetTagHelperProperty(CSharpRenderingContext context, SetTagHelperPropertyIRNode node);
    }
}
