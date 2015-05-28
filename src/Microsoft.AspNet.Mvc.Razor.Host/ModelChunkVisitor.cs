// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.CodeGenerators.Visitors;
using Microsoft.Framework.Internal;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class ModelChunkVisitor : MvcCSharpCodeVisitor
    {
        public ModelChunkVisitor(
            [NotNull] CSharpCodeWriter writer,
            [NotNull] CodeGeneratorContext context)
            : base(writer, context)
        {
        }

        protected override void Visit(ModelChunk chunk)
        {
            var csharpVisitor = new CSharpCodeVisitor(Writer, Context);

            Writer.Write(chunk.BaseType).Write("<");
            csharpVisitor.CreateExpressionCodeMapping(chunk.ModelType, chunk);
            Writer.Write(">");
        }
    }
}