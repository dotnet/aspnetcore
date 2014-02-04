using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpClassAttributeVisitor : CodeVisitor<CSharpCodeWriter>
    {
        public CSharpClassAttributeVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
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
