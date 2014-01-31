using Microsoft.AspNet.Razor.Parser.SyntaxTree;
using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class Chunk
    {
        public SourceLocation Start { get; set; }
        public SyntaxTreeNode Association { get; set; }
        public string WriterName { get; set; }
    }
}
