// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNet.Razor.CodeGenerators;
using Microsoft.AspNet.Razor.CodeGenerators.Visitors;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class ModelChunkVisitor : MvcCSharpCodeVisitor
    {
        public ModelChunkVisitor(
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
            : base(writer, context)
        {
            if (writer == null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
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