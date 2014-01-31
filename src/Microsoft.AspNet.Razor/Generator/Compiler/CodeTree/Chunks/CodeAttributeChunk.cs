using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class CodeAttributeChunk : ChunkBlock
    {
        public string Attribute { get; set; }
        public LocationTagged<string> Prefix { get; set; }
        public LocationTagged<string> Suffix { get; set; }
    }
}
