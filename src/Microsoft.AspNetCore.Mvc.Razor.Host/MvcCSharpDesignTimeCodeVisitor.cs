// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Chunks;
using Microsoft.AspNetCore.Razor.CodeGenerators;
using Microsoft.AspNetCore.Razor.CodeGenerators.Visitors;

namespace Microsoft.AspNetCore.Mvc.Razor
{
    public class MvcCSharpDesignTimeCodeVisitor : CSharpDesignTimeCodeVisitor
    {
        private const string ModelVariable = "__modelHelper";
        private ModelChunk _modelChunk;

        public MvcCSharpDesignTimeCodeVisitor(
            CSharpCodeVisitor csharpCodeVisitor,
            CSharpCodeWriter writer,
            CodeGeneratorContext context)
            : base(csharpCodeVisitor, writer, context)
        {
        }

        protected override void AcceptTreeCore(ChunkTree tree)
        {
            base.AcceptTreeCore(tree);

            if (_modelChunk != null)
            {
                WriteModelChunkLineMapping();
            }
        }

        public override void Accept(Chunk chunk)
        {
            if (chunk is ModelChunk)
            {
                Visit((ModelChunk)chunk);
            }

            base.Accept(chunk);
        }

        private void Visit(ModelChunk chunk)
        {
            Debug.Assert(chunk != null);
            _modelChunk = chunk;
        }

        private void WriteModelChunkLineMapping()
        {
            Debug.Assert(Context.Host.DesignTimeMode);

            using (var lineMappingWriter =
                Writer.BuildLineMapping(_modelChunk.Start, _modelChunk.ModelType.Length, Context.SourceFile))
            {
                // var __modelHelper = default(MyModel);
                Writer.Write("var ")
                    .Write(ModelVariable)
                    .Write(" = default(");

                lineMappingWriter.MarkLineMappingStart();
                Writer.Write(_modelChunk.ModelType);
                lineMappingWriter.MarkLineMappingEnd();

                Writer.WriteLine(");");
            }
        }
    }
}
