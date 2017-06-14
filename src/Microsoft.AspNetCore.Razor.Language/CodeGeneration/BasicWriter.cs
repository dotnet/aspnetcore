// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class BasicWriter
    {
        public abstract void WriteUsingStatement(CSharpRenderingContext context, UsingStatementIRNode node);

        public abstract void WriteCSharpExpression(CSharpRenderingContext context, CSharpExpressionIRNode node);

        public abstract void WriteCSharpCode(CSharpRenderingContext context, CSharpCodeIRNode node);

        public abstract void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIRNode node);

        public abstract void WriteHtmlAttribute(CSharpRenderingContext context, HtmlAttributeIRNode node);

        public abstract void WriteHtmlAttributeValue(CSharpRenderingContext context, HtmlAttributeValueIRNode node);

        public abstract void WriteCSharpExpressionAttributeValue(CSharpRenderingContext context, CSharpExpressionAttributeValueIRNode node);

        public abstract void WriteCSharpCodeAttributeValue(CSharpRenderingContext context, CSharpCodeAttributeValueIRNode node);

        public abstract void BeginWriterScope(CSharpRenderingContext context, string writer);

        public abstract void EndWriterScope(CSharpRenderingContext context);
    }
}
