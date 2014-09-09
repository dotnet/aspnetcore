// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpClassAttributeVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public CSharpClassAttributeVisitor(CSharpCodeWriter writer, CodeBuilderContext context)
            : base(writer, context) { }

        protected override void Visit(SessionStateChunk chunk)
        {
            Writer.Write("[")
                  .Write(typeof(RazorDirectiveAttribute).FullName)
                  .Write("(")
                  .WriteStringLiteral(SyntaxConstants.CSharp.SessionStateKeyword)
                  .WriteParameterSeparator()
                  .WriteStringLiteral(chunk.Value)
                  .WriteLine(")]");
        }
    }
}
