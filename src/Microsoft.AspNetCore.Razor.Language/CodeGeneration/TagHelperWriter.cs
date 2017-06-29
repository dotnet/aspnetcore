// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    internal abstract class TagHelperWriter
    {
        public abstract void WriteDeclareTagHelperFields(CodeRenderingContext context, DeclareTagHelperFieldsIntermediateNode node);

        public abstract void WriteTagHelper(CodeRenderingContext context, TagHelperIntermediateNode node);

        public abstract void WriteTagHelperBody(CodeRenderingContext context, TagHelperBodyIntermediateNode node);

        public abstract void WriteCreateTagHelper(CodeRenderingContext context, CreateTagHelperIntermediateNode node);

        public abstract void WriteAddTagHelperHtmlAttribute(CodeRenderingContext context, AddTagHelperHtmlAttributeIntermediateNode node);

        public abstract void WriteSetTagHelperProperty(CodeRenderingContext context, SetTagHelperPropertyIntermediateNode node);
    }
}
