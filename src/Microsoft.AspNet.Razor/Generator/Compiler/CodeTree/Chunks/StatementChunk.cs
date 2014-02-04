
namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class StatementChunk : Chunk
    {
        public string Code { get; set; }

        public override string ToString()
        {
            return Start + " = " + Code;
        }
    }
}
