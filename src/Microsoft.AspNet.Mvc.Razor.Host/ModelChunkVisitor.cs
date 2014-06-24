using Microsoft.AspNet.Razor.Generator;
using Microsoft.AspNet.Razor.Generator.Compiler.CSharp;

namespace Microsoft.AspNet.Mvc.Razor
{
    public class ModelChunkVisitor : MvcCSharpCodeVisitor
    {
        public ModelChunkVisitor([NotNull] CSharpCodeWriter writer,
                                 [NotNull] CodeGeneratorContext context)
            : base(writer, context)
        { }

        protected override void Visit(ModelChunk chunk)
        {
            var csharpVisitor = new CSharpCodeVisitor(Writer, Context);

            Writer.Write(chunk.BaseType).Write("<");
            csharpVisitor.CreateExpressionCodeMapping(chunk.ModelType, chunk);
            Writer.Write(">");
        }
    }
}