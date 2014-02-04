
namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class ExpressionChunk : Chunk
    {
        public string Code { get; set; }
        public ExpressionRenderingMode RenderingMode { get; set; }

        public override string ToString()
        {
            return Start + " = " + Code;
        }
    }
}
