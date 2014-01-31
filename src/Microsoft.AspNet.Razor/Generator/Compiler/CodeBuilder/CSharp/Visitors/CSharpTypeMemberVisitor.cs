using System.Linq;

namespace Microsoft.AspNet.Razor.Generator.Compiler.CSharp
{
    public class CSharpTypeMemberVisitor : CodeVisitor
    {
        private CSharpCodeWriter _writer;
        private string _sourceFile;

        public CSharpTypeMemberVisitor(CSharpCodeWriter writer, string sourceFile)
        {
            _writer = writer;
            _sourceFile = sourceFile;
        }

        protected override void Visit(TypeMemberChunk chunk)
        {
            Snippet code = chunk.Code.FirstOrDefault();

            if (code != null)
            {
                using (_writer.BuildLineMapping(chunk.Start, code.Value.Length, _sourceFile))
                {
                    _writer.WriteLine(code.Value);
                }
            }
        }
    }
}
