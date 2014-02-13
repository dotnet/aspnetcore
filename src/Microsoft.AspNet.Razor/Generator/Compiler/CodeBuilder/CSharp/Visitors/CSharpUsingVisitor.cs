using System.Collections.Generic;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpUsingVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public CSharpUsingVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
            : base(writer, context)
        {
            ImportedUsings = new List<string>();
        }

        public IList<string> ImportedUsings { get; set; }

        protected override void Visit(UsingChunk chunk)
        {
            ImportedUsings.Add(chunk.Namespace);

            using (Writer.BuildLineMapping(chunk.Start, chunk.Association.Length, Context.SourceFile))
            {
                Writer.WriteUsing(chunk.Namespace, endLine: false);
            }

            Writer.WriteLine(";");
        }
    }
}
