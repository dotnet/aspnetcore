// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class BasicWriter
    {
        public abstract void WriteUsingStatement(CSharpRenderingContext context, UsingStatementIntermediateNode node);

        public abstract void WriteCSharpExpression(CSharpRenderingContext context, CSharpExpressionIntermediateNode node);

        public abstract void WriteCSharpCode(CSharpRenderingContext context, CSharpCodeIntermediateNode node);

        public abstract void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIntermediateNode node);

        public abstract void WriteHtmlAttribute(CSharpRenderingContext context, HtmlAttributeIntermediateNode node);

        public abstract void WriteHtmlAttributeValue(CSharpRenderingContext context, HtmlAttributeValueIntermediateNode node);

        public abstract void WriteCSharpExpressionAttributeValue(CSharpRenderingContext context, CSharpExpressionAttributeValueIntermediateNode node);

        public abstract void WriteCSharpCodeAttributeValue(CSharpRenderingContext context, CSharpCodeAttributeValueIntermediateNode node);

        public abstract void BeginWriterScope(CSharpRenderingContext context, string writer);

        public abstract void EndWriterScope(CSharpRenderingContext context);
    }
}
