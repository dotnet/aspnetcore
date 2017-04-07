// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Razor.Language.Intermediate;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration
{
    public abstract class BasicWriter
    {
        public abstract void WriteCSharpExpression(CSharpRenderingContext context, CSharpExpressionIRNode node);

        public abstract void WriteCSharpStatement(CSharpRenderingContext context, CSharpStatementIRNode node);

        public abstract void WriteHtmlContent(CSharpRenderingContext context, HtmlContentIRNode node);

        public abstract void WriteHtmlAttribute(CSharpRenderingContext context, HtmlAttributeIRNode node);
    }
}
