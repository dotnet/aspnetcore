using Microsoft.AspNet.Razor.Text;

namespace Microsoft.AspNet.Razor.Generator.Compiler
{
    public class HelperChunk : ChunkBlock
    {
        public LocationTagged<string> Signature { get; set; }
        public LocationTagged<string> Footer { get; set; }
        public bool HeaderComplete { get; set; }
    }
}
