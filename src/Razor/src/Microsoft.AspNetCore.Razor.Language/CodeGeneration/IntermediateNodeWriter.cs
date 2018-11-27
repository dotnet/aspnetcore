// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class IntermediateNodeWriter
    {
        public abstract void WriteUsingDirective(CodeRenderingContext context, UsingDirectiveIntermediateNode node);

        public abstract void WriteCSharpExpression(CodeRenderingContext context, CSharpExpressionIntermediateNode node);

        public abstract void WriteCSharpCode(CodeRenderingContext context, CSharpCodeIntermediateNode node);

        public abstract void WriteHtmlContent(CodeRenderingContext context, HtmlContentIntermediateNode node);

        public abstract void WriteHtmlAttribute(CodeRenderingContext context, HtmlAttributeIntermediateNode node);

        public abstract void WriteHtmlAttributeValue(CodeRenderingContext context, HtmlAttributeValueIntermediateNode node);

        public abstract void WriteCSharpExpressionAttributeValue(CodeRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node);

        public abstract void WriteCSharpCodeAttributeValue(CodeRenderingContext context, CSharpCodeAttributeValueIntermediateNode node);

        public abstract void BeginWriterScope(CodeRenderingContext context, string writer);

        public abstract void EndWriterScope(CodeRenderingContext context);
    }
}
