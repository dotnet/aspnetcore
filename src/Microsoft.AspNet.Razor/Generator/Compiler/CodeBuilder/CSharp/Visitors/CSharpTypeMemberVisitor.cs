using System.Linq;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpTypeMemberVisitor : CodeVisitor
    {
        private CSharpCodeWriter _writer;
        private CodeGeneratorContext _context;

        public CSharpTypeMemberVisitor(CSharpCodeWriter writer, CodeGeneratorContext context)
        {
            _writer = writer;
            _context = context;
        }

        protected override void Visit(TypeMemberChunk chunk)
        {
            Snippet code = chunk.Code.FirstOrDefault();

            if (code != null)
            {
                using (_writer.BuildLineMapping(chunk.Start, code.Value.Length, _context.SourceFile))
                {
                    _writer.WriteLine(code.Value);
                }
            }
        }
    }
}
