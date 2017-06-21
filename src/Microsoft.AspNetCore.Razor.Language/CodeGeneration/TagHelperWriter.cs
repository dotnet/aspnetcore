// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class TagHelperWriter
    {
        public abstract void WriteDeclareTagHelperFields(CSharpRenderingContext context, DeclareTagHelperFieldsIntermediateNode node);

        public abstract void WriteTagHelper(CSharpRenderingContext context, TagHelperIntermediateNode node);

        public abstract void WriteTagHelperBody(CSharpRenderingContext context, TagHelperBodyIntermediateNode node);

        public abstract void WriteCreateTagHelper(CSharpRenderingContext context, CreateTagHelperIntermediateNode node);

        public abstract void WriteAddTagHelperHtmlAttribute(CSharpRenderingContext context, AddTagHelperHtmlAttributeIntermediateNode node);

        public abstract void WriteSetTagHelperProperty(CSharpRenderingContext context, SetTagHelperPropertyIntermediateNode node);
    }
}
