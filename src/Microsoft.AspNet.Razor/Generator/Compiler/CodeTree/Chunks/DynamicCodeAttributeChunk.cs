using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class DynamicCodeAttributeChunk : ChunkBlock
    {
        public LocationTagged<string> Prefix { get; set; }
    }
}
