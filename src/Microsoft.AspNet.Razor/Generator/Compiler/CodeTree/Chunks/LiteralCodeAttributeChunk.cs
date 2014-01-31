using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class LiteralCodeAttributeChunk : ChunkBlock
    {
        public Snippet Code { get; set; }
        public LocationTagged<string> Prefix { get; set; }
        public LocationTagged<string> Value { get; set; }
        public SourceLocation ValueLocation { get; set; }
    }
}
