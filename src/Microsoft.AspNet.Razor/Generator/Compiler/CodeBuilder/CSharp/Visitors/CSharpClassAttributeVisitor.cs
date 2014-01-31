using Microsoft.AspNet.Razor.Parser;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpClassAttributeVisitor : CodeVisitor
    {
        private CSharpCodeWriter _writer;

        public CSharpClassAttributeVisitor(CSharpCodeWriter writer)
        {
            _writer = writer;
        }

        protected override void Visit(SessionStateChunk chunk)
        {
            _writer.Write("[")
                   .Write(typeof(RazorDirectiveAttribute).FullName)
                   .Write("(")
                   .WriteStringLiteral(SyntaxConstants.CSharp.SessionStateKeyword)
                   .WriteParameterSeparator()
                   .WriteStringLiteral(chunk.Value)
                   .WriteLine(")]");
        }
    }
}
