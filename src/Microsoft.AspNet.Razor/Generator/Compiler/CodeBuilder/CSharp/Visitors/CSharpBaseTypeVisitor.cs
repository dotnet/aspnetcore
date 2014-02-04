
namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpBaseTypeVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public CSharpBaseTypeVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
            : base(writer, context) { }

        public string CurrentBaseType { get; set; }

        protected override void Visit(SetBaseTypeChunk chunk)
        {
            CurrentBaseType = chunk.TypeName;
        }
    }
}
