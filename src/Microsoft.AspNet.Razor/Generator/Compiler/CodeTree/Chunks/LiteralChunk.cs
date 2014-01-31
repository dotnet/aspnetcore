
namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class LiteralChunk : Chunk
    {
        public string Text { get; set; }

        public override string ToString()
        {
            return Start + " = " + Text;
        }
    }
}
